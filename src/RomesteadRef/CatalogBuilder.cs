using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace RomesteadRef;

internal sealed class CatalogBuilder
{
    private static readonly string[] DefaultAssemblyPrefixes =
    [
        "Romestead",
        "Candide",
        "Shared"
    ];

    public CatalogBuildResult Build(CatalogBuildOptions options)
    {
        var includedPaths = ResolveAssemblyPaths(options).ToArray();
        var resolver = new DefaultAssemblyResolver();
        foreach (var directory in includedPaths
                     .Select(Path.GetDirectoryName)
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            resolver.AddSearchDirectory(directory!);
        }

        var assemblies = new List<AssemblyCatalog>();
        var skipped = new List<SkippedAssembly>();

        foreach (var path in includedPaths)
        {
            try
            {
                assemblies.Add(BuildAssemblyCatalog(path, resolver, options.BaseDirectory, options.IncludeCompilerGenerated));
            }
            catch (Exception ex)
            {
                skipped.Add(new SkippedAssembly(path, $"{ex.GetType().Name}: {ex.Message}"));
            }
        }

        assemblies.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name));

        var metadata = new CatalogMetadata(
            GeneratedAtUtc: DateTime.UtcNow,
            BaseDirectory: options.BaseDirectory,
            InputRoots: options.InputRoots,
            AssemblyFilters: options.AssemblyNameFilters,
            IncludeSystemAssemblies: options.IncludeSystemAssemblies,
            IncludeCompilerGenerated: options.IncludeCompilerGenerated,
            AssemblyCount: assemblies.Count,
            TypeCount: assemblies.Sum(assembly => assembly.TypeCount),
            MethodCount: assemblies.Sum(assembly => assembly.MethodCount),
            PropertyCount: assemblies.Sum(assembly => assembly.PropertyCount),
            FieldCount: assemblies.Sum(assembly => assembly.FieldCount));

        return new CatalogBuildResult(
            new CatalogSnapshot(metadata, assemblies),
            includedPaths,
            skipped);
    }

    private static IEnumerable<string> ResolveAssemblyPaths(CatalogBuildOptions options)
    {
        var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in options.InputRoots)
        {
            if (File.Exists(root) && Path.GetExtension(root).Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                AddCandidate(root);
                continue;
            }

            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var path in Directory.GetFiles(root, "*.dll", SearchOption.TopDirectoryOnly))
            {
                AddCandidate(path);
            }
        }

        return byName.Values.OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (!ShouldIncludeAssembly(fileName, options))
            {
                return;
            }

            if (!byName.TryGetValue(fileName, out var existing) ||
                path.Length < existing.Length)
            {
                byName[fileName] = Path.GetFullPath(path);
            }
        }
    }

    private static bool ShouldIncludeAssembly(string assemblyName, CatalogBuildOptions options)
    {
        if (options.ExactAssemblyNameFilters.Count > 0)
        {
            return options.ExactAssemblyNameFilters.Contains(assemblyName, StringComparer.OrdinalIgnoreCase);
        }

        if (options.AssemblyNameFilters.Count > 0)
        {
            return options.AssemblyNameFilters.Any(filter =>
                assemblyName.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        if (options.IncludeSystemAssemblies)
        {
            return true;
        }

        return DefaultAssemblyPrefixes.Any(prefix =>
            assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static AssemblyCatalog BuildAssemblyCatalog(
        string path,
        IAssemblyResolver resolver,
        string baseDirectory,
        bool includeCompilerGenerated)
    {
        var readerParameters = new ReaderParameters
        {
            AssemblyResolver = resolver,
            ReadingMode = ReadingMode.Deferred,
            ReadSymbols = File.Exists(Path.ChangeExtension(path, ".pdb"))
        };

        using var assembly = AssemblyDefinition.ReadAssembly(path, readerParameters);
        var typeCatalogs = FlattenTypes(assembly.MainModule.Types)
            .Where(type => type.Name != "<Module>")
            .Where(type => includeCompilerGenerated || !IsCompilerGenerated(type))
            .Select(type => BuildTypeCatalog(type, baseDirectory, includeCompilerGenerated))
            .OrderBy(type => type.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var namespaces = typeCatalogs
            .GroupBy(type => string.IsNullOrWhiteSpace(type.Namespace) ? "<global>" : type.Namespace, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new NamespaceCatalog(
                Name: group.Key,
                TypeIds: group.Select(type => type.Id).ToArray(),
                TypeCount: group.Count(),
                MethodCount: group.Sum(type => type.Methods.Count)))
            .ToArray();

        var references = assembly.MainModule.AssemblyReferences
            .Select(reference => reference.FullName)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new AssemblyCatalog(
            Name: assembly.Name.Name ?? Path.GetFileNameWithoutExtension(path),
            Version: assembly.Name.Version?.ToString() ?? "0.0.0.0",
            FilePath: path,
            ModuleVersionId: assembly.MainModule.Mvid.ToString(),
            Hash: ComputeHash(
                string.Join(
                    "\n",
                    typeCatalogs.Select(type => $"{type.Id}|{type.Hash}"))),
            References: references,
            Namespaces: namespaces,
            Types: typeCatalogs,
            TypeCount: typeCatalogs.Length,
            MethodCount: typeCatalogs.Sum(type => type.Methods.Count),
            PropertyCount: typeCatalogs.Sum(type => type.Properties.Count),
            FieldCount: typeCatalogs.Sum(type => type.Fields.Count));
    }

    private static TypeCatalog BuildTypeCatalog(TypeDefinition type, string baseDirectory, bool includeCompilerGenerated)
    {
        var fields = type.Fields
            .Where(field => includeCompilerGenerated || !LooksCompilerGenerated(field.Name))
            .Select(BuildFieldCatalog)
            .OrderBy(field => field.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var properties = type.Properties
            .Where(property => includeCompilerGenerated || !LooksCompilerGenerated(property.Name))
            .Select(BuildPropertyCatalog)
            .OrderBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var events = type.Events
            .Where(eventInfo => includeCompilerGenerated || !LooksCompilerGenerated(eventInfo.Name))
            .Select(BuildEventCatalog)
            .OrderBy(eventInfo => eventInfo.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var methods = type.Methods
            .Where(method => includeCompilerGenerated || (!method.IsSpecialName && !LooksCompilerGenerated(method.Name)))
            .Select(method => BuildMethodCatalog(method, baseDirectory))
            .OrderBy(method => method.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(method => method.Signature, StringComparer.Ordinal)
            .ToArray();

        var sources = methods
            .Select(method => method.Source)
            .Where(source => source is not null)
            .Cast<SourceLocation>()
            .Distinct()
            .OrderBy(source => source.DocumentPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(source => source.StartLine)
            .ToArray();

        var interfaces = type.Interfaces
            .Select(@interface => FormatTypeName(@interface.InterfaceType))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var visibility = GetTypeVisibility(type);
        var kind = GetTypeKind(type);
        var baseType = type.BaseType is null ? null : FormatTypeName(type.BaseType);
        var displayName = FormatTypeName(type, includeNamespace: false);
        var fullName = FormatTypeName(type);
        var id = GetStableTypeId(type);
        var hashSource = new StringBuilder();
        hashSource.Append(kind).Append('|').Append(visibility).Append('|').Append(baseType).AppendLine();
        foreach (var @interface in interfaces)
        {
            hashSource.Append("I|").Append(@interface).AppendLine();
        }

        foreach (var field in fields)
        {
            hashSource.Append("F|").Append(field.Id).Append('|').Append(field.Hash).AppendLine();
        }

        foreach (var property in properties)
        {
            hashSource.Append("P|").Append(property.Id).Append('|').Append(property.Hash).AppendLine();
        }

        foreach (var eventInfo in events)
        {
            hashSource.Append("E|").Append(eventInfo.Id).Append('|').Append(eventInfo.Hash).AppendLine();
        }

        foreach (var method in methods)
        {
            hashSource.Append("M|").Append(method.Id).Append('|').Append(method.Hash).AppendLine();
        }

        return new TypeCatalog(
            Id: id,
            Name: type.Name,
            DisplayName: displayName,
            FullName: fullName,
            Namespace: string.IsNullOrWhiteSpace(type.Namespace) ? "<global>" : type.Namespace,
            Kind: kind,
            Visibility: visibility,
            IsAbstract: type.IsAbstract,
            IsSealed: type.IsSealed,
            BaseType: baseType,
            Interfaces: interfaces,
            SourceLocations: sources,
            Fields: fields,
            Properties: properties,
            Events: events,
            Methods: methods,
            Hash: ComputeHash(hashSource.ToString()));
    }

    private static FieldCatalog BuildFieldCatalog(FieldDefinition field)
    {
        var signature = $"{GetFieldVisibility(field)} {FormatTypeName(field.FieldType)} {field.Name}";
        var id = $"{field.Name}:{GetStableTypeId(field.FieldType)}";
        return new FieldCatalog(
            Id: id,
            Name: field.Name,
            Type: FormatTypeName(field.FieldType),
            Signature: signature,
            Visibility: GetFieldVisibility(field),
            IsStatic: field.IsStatic,
            IsLiteral: field.IsLiteral,
            Hash: ComputeHash(signature));
    }

    private static PropertyCatalog BuildPropertyCatalog(PropertyDefinition property)
    {
        var getter = property.GetMethod;
        var setter = property.SetMethod;
        var visibility = getter is not null ? GetMethodVisibility(getter) : setter is not null ? GetMethodVisibility(setter) : "private";
        var signature = $"{visibility} {FormatTypeName(property.PropertyType)} {property.Name}";
        var id = $"{property.Name}:{GetStableTypeId(property.PropertyType)}";
        return new PropertyCatalog(
            Id: id,
            Name: property.Name,
            Type: FormatTypeName(property.PropertyType),
            Signature: signature,
            Visibility: visibility,
            HasGetter: getter is not null,
            HasSetter: setter is not null,
            Hash: ComputeHash($"{signature}|get={getter is not null}|set={setter is not null}"));
    }

    private static EventCatalog BuildEventCatalog(EventDefinition eventInfo)
    {
        var method = eventInfo.AddMethod ?? eventInfo.RemoveMethod;
        var visibility = method is not null ? GetMethodVisibility(method) : "private";
        var signature = $"{visibility} {FormatTypeName(eventInfo.EventType)} {eventInfo.Name}";
        return new EventCatalog(
            Id: $"{eventInfo.Name}:{GetStableTypeId(eventInfo.EventType)}",
            Name: eventInfo.Name,
            Type: FormatTypeName(eventInfo.EventType),
            Signature: signature,
            Visibility: visibility,
            Hash: ComputeHash(signature));
    }

    private static MethodCatalog BuildMethodCatalog(MethodDefinition method, string baseDirectory)
    {
        var parameters = method.Parameters.Select(parameter => new ParameterCatalog(
                Name: string.IsNullOrWhiteSpace(parameter.Name) ? $"arg{parameter.Index}" : parameter.Name,
                Type: FormatTypeName(parameter.ParameterType),
                IsOut: parameter.IsOut,
                IsOptional: parameter.IsOptional))
            .ToArray();

        var signature = BuildMethodSignature(method, parameters);
        var id = BuildMethodId(method, parameters);
        var bodySummary = SummarizeMethodBody(method);
        var source = GetSourceLocation(method, baseDirectory);
        var hash = ComputeHash(
            string.Join(
                "|",
                signature,
                GetMethodVisibility(method),
                method.IsStatic,
                method.IsAbstract,
                method.IsVirtual,
                method.GenericParameters.Count,
                bodySummary.BodyHash ?? "<none>"));

        return new MethodCatalog(
            Id: id,
            Name: method.IsConstructor ? (method.IsStatic ? ".cctor" : ".ctor") : method.Name,
            Signature: signature,
            Visibility: GetMethodVisibility(method),
            ReturnType: method.IsConstructor ? "void" : FormatTypeName(method.ReturnType),
            Parameters: parameters,
            IsStatic: method.IsStatic,
            IsAbstract: method.IsAbstract,
            IsVirtual: method.IsVirtual,
            IsConstructor: method.IsConstructor,
            GenericArity: method.GenericParameters.Count,
            IlSize: bodySummary.IlSize,
            BodyHash: bodySummary.BodyHash,
            Hash: hash,
            CalledMethods: bodySummary.CalledMethods,
            StringLiterals: bodySummary.StringLiterals,
            Source: source);
    }

    private static SourceLocation? GetSourceLocation(MethodDefinition method, string baseDirectory)
    {
        if (!method.DebugInformation.HasSequencePoints)
        {
            return null;
        }

        var point = method.DebugInformation.SequencePoints.FirstOrDefault(sequencePoint => !sequencePoint.IsHidden);
        if (point?.Document?.Url is null)
        {
            return null;
        }

        var path = point.Document.Url.Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(path) &&
            path.StartsWith(baseDirectory, StringComparison.OrdinalIgnoreCase))
        {
            path = Path.GetRelativePath(baseDirectory, path);
        }

        return new SourceLocation(path, point.StartLine > 0 ? point.StartLine : null);
    }

    private static MethodBodySummary SummarizeMethodBody(MethodDefinition method)
    {
        if (!method.HasBody)
        {
            return new MethodBodySummary(0, null, [], []);
        }

        var normalized = new StringBuilder();
        var calledMethods = new List<string>();
        var stringLiterals = new List<string>();

        foreach (var instruction in method.Body.Instructions)
        {
            normalized.Append(instruction.OpCode.Code).Append('|');
            normalized.Append(NormalizeOperand(instruction.Operand)).AppendLine();

            switch (instruction.Operand)
            {
                case MethodReference methodReference:
                    AddDistinct(calledMethods, FormatMethodReference(methodReference), 32);
                    break;
                case string text when !string.IsNullOrWhiteSpace(text):
                    AddDistinct(stringLiterals, text.Length > 120 ? text[..120] : text, 16);
                    break;
            }
        }

        return new MethodBodySummary(
            method.Body.CodeSize,
            ComputeHash(normalized.ToString()),
            calledMethods,
            stringLiterals);
    }

    private static void AddDistinct(List<string> items, string value, int maxCount)
    {
        if (items.Count >= maxCount || items.Contains(value, StringComparer.Ordinal))
        {
            return;
        }

        items.Add(value);
    }

    private static string NormalizeOperand(object? operand) =>
        operand switch
        {
            null => "",
            MethodReference methodReference => FormatMethodReference(methodReference),
            FieldReference fieldReference => $"{FormatTypeName(fieldReference.DeclaringType)}::{fieldReference.Name}",
            TypeReference typeReference => FormatTypeName(typeReference),
            ParameterDefinition parameterDefinition => $"arg:{parameterDefinition.Index}:{parameterDefinition.Name}",
            VariableDefinition variableDefinition => $"var:{variableDefinition.Index}:{FormatTypeName(variableDefinition.VariableType)}",
            Instruction => "target",
            Instruction[] instructions => $"targets:{instructions.Length}",
            string text => text,
            sbyte value => value.ToString(CultureInfo.InvariantCulture),
            byte value => value.ToString(CultureInfo.InvariantCulture),
            short value => value.ToString(CultureInfo.InvariantCulture),
            ushort value => value.ToString(CultureInfo.InvariantCulture),
            int value => value.ToString(CultureInfo.InvariantCulture),
            uint value => value.ToString(CultureInfo.InvariantCulture),
            long value => value.ToString(CultureInfo.InvariantCulture),
            ulong value => value.ToString(CultureInfo.InvariantCulture),
            float value => value.ToString("R", CultureInfo.InvariantCulture),
            double value => value.ToString("R", CultureInfo.InvariantCulture),
            CallSite callSite => $"{FormatTypeName(callSite.ReturnType)}({string.Join(", ", callSite.Parameters.Select(parameter => FormatTypeName(parameter.ParameterType)))})",
            _ => operand.ToString() ?? ""
        };

    private static string BuildMethodSignature(MethodDefinition method, IReadOnlyList<ParameterCatalog> parameters)
    {
        var returnType = method.IsConstructor ? "void" : FormatTypeName(method.ReturnType);
        var name = method.IsConstructor
            ? (method.IsStatic ? ".cctor" : ".ctor")
            : method.Name;
        var genericSuffix = method.GenericParameters.Count > 0
            ? $"<{string.Join(", ", method.GenericParameters.Select(parameter => parameter.Name))}>"
            : "";

        return $"{GetMethodVisibility(method)} {returnType} {name}{genericSuffix}({string.Join(", ", parameters.Select(FormatParameter))})";
    }

    private static string BuildMethodId(MethodDefinition method, IReadOnlyList<ParameterCatalog> parameters)
    {
        var name = method.IsConstructor
            ? (method.IsStatic ? ".cctor" : ".ctor")
            : method.Name;
        var genericSuffix = method.GenericParameters.Count > 0 ? $"`{method.GenericParameters.Count}" : "";
        var parameterTypes = string.Join(", ", method.Parameters.Select(parameter => GetStableTypeId(parameter.ParameterType)));
        return $"{name}{genericSuffix}({parameterTypes})->{(method.IsConstructor ? "void" : GetStableTypeId(method.ReturnType))}";
    }

    private static string FormatParameter(ParameterCatalog parameter)
    {
        var modifier = parameter.IsOut ? "out " : "";
        return $"{modifier}{parameter.Type} {parameter.Name}";
    }

    private static string FormatMethodReference(MethodReference methodReference)
    {
        var genericSuffix = methodReference.GenericParameters.Count > 0 ? $"`{methodReference.GenericParameters.Count}" : "";
        return $"{FormatTypeName(methodReference.DeclaringType)}::{methodReference.Name}{genericSuffix}({string.Join(", ", methodReference.Parameters.Select(parameter => FormatTypeName(parameter.ParameterType)))})";
    }

    private static string FormatTypeName(TypeReference? type, bool includeNamespace = true)
    {
        if (type is null)
        {
            return "<null>";
        }

        return type switch
        {
            GenericParameter genericParameter => genericParameter.Name,
            ByReferenceType byReferenceType => $"{FormatTypeName(byReferenceType.ElementType, includeNamespace)}&",
            PointerType pointerType => $"{FormatTypeName(pointerType.ElementType, includeNamespace)}*",
            ArrayType arrayType => $"{FormatTypeName(arrayType.ElementType, includeNamespace)}[{new string(',', Math.Max(0, arrayType.Rank - 1))}]",
            OptionalModifierType optionalModifierType => FormatTypeName(optionalModifierType.ElementType, includeNamespace),
            RequiredModifierType requiredModifierType => FormatTypeName(requiredModifierType.ElementType, includeNamespace),
            GenericInstanceType genericInstanceType => FormatGenericInstance(genericInstanceType, includeNamespace),
            TypeSpecification typeSpecification => FormatTypeName(typeSpecification.ElementType, includeNamespace),
            _ => FormatSimpleTypeName(type, includeNamespace)
        };
    }

    private static string GetStableTypeId(TypeReference? type)
    {
        if (type is null)
        {
            return "<null>";
        }

        return type switch
        {
            GenericParameter genericParameter => $"!{genericParameter.Name}",
            ByReferenceType byReferenceType => $"{GetStableTypeId(byReferenceType.ElementType)}&",
            PointerType pointerType => $"{GetStableTypeId(pointerType.ElementType)}*",
            ArrayType arrayType => $"{GetStableTypeId(arrayType.ElementType)}[{new string(',', Math.Max(0, arrayType.Rank - 1))}]",
            OptionalModifierType optionalModifierType => GetStableTypeId(optionalModifierType.ElementType),
            RequiredModifierType requiredModifierType => GetStableTypeId(requiredModifierType.ElementType),
            GenericInstanceType genericInstanceType => $"{GetStableTypeId(genericInstanceType.ElementType)}<{string.Join(",", genericInstanceType.GenericArguments.Select(GetStableTypeId))}>",
            TypeSpecification typeSpecification => GetStableTypeId(typeSpecification.ElementType),
            _ => BuildStableSimpleTypeId(type)
        };
    }

    private static string FormatGenericInstance(GenericInstanceType type, bool includeNamespace)
    {
        var baseName = FormatSimpleTypeName(type.ElementType, includeNamespace);
        var tickIndex = baseName.IndexOf('`');
        if (tickIndex >= 0)
        {
            baseName = baseName[..tickIndex];
        }

        return $"{baseName}<{string.Join(", ", type.GenericArguments.Select(argument => FormatTypeName(argument, includeNamespace)))}>";
    }

    private static string FormatSimpleTypeName(TypeReference type, bool includeNamespace)
    {
        var parts = new Stack<string>();
        TypeReference? current = type;

        while (current is not null)
        {
            var name = current.Name;
            var tickIndex = name.IndexOf('`');
            if (tickIndex >= 0)
            {
                name = name[..tickIndex];
            }

            parts.Push(name);
            current = current.DeclaringType;
        }

        var combinedName = string.Join(".", parts);
        if (!includeNamespace || string.IsNullOrWhiteSpace(type.Namespace))
        {
            return combinedName;
        }

        return $"{type.Namespace}.{combinedName}";
    }

    private static string BuildStableSimpleTypeId(TypeReference type)
    {
        var parts = new Stack<string>();
        TypeReference? current = type;

        while (current is not null)
        {
            parts.Push(current.Name);
            current = current.DeclaringType;
        }

        var combinedName = string.Join("+", parts);
        if (string.IsNullOrWhiteSpace(type.Namespace))
        {
            return combinedName;
        }

        return $"{type.Namespace}.{combinedName}";
    }

    private static string GetTypeKind(TypeDefinition type)
    {
        if (type.IsEnum)
        {
            return "enum";
        }

        if (type.IsInterface)
        {
            return "interface";
        }

        if (type.BaseType?.FullName == "System.MulticastDelegate")
        {
            return "delegate";
        }

        if (type.IsValueType)
        {
            return "struct";
        }

        return "class";
    }

    private static string GetTypeVisibility(TypeDefinition type)
    {
        if (type.IsPublic || type.IsNestedPublic)
        {
            return "public";
        }

        if (type.IsNestedFamily)
        {
            return "protected";
        }

        if (type.IsNestedFamilyOrAssembly)
        {
            return "protected internal";
        }

        if (type.IsNestedFamilyAndAssembly)
        {
            return "private protected";
        }

        if (type.IsNotPublic || type.IsNestedAssembly)
        {
            return "internal";
        }

        return "private";
    }

    private static string GetFieldVisibility(FieldDefinition field)
    {
        if (field.IsPublic)
        {
            return "public";
        }

        if (field.IsFamily)
        {
            return "protected";
        }

        if (field.IsFamilyOrAssembly)
        {
            return "protected internal";
        }

        if (field.IsFamilyAndAssembly)
        {
            return "private protected";
        }

        if (field.IsAssembly)
        {
            return "internal";
        }

        return "private";
    }

    private static string GetMethodVisibility(MethodDefinition method)
    {
        if (method.IsPublic)
        {
            return "public";
        }

        if (method.IsFamily)
        {
            return "protected";
        }

        if (method.IsFamilyOrAssembly)
        {
            return "protected internal";
        }

        if (method.IsFamilyAndAssembly)
        {
            return "private protected";
        }

        if (method.IsAssembly)
        {
            return "internal";
        }

        return "private";
    }

    private static IEnumerable<TypeDefinition> FlattenTypes(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            yield return type;

            foreach (var nestedType in FlattenTypes(type.NestedTypes))
            {
                yield return nestedType;
            }
        }
    }

    private static bool IsCompilerGenerated(ICustomAttributeProvider provider) =>
        provider.CustomAttributes.Any(attribute =>
            attribute.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");

    private static bool IsCompilerGenerated(TypeDefinition type) =>
        IsCompilerGenerated((ICustomAttributeProvider)type) ||
        LooksCompilerGenerated(type.Name) ||
        LooksCompilerGenerated(type.FullName);

    private static bool LooksCompilerGenerated(string? name) =>
        !string.IsNullOrWhiteSpace(name) &&
        (name.Contains('<', StringComparison.Ordinal) ||
         name.Contains('>', StringComparison.Ordinal) ||
         name.Contains("AnonymousType", StringComparison.Ordinal));

    private static string ComputeHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }

    private sealed record MethodBodySummary(
        int IlSize,
        string? BodyHash,
        IReadOnlyList<string> CalledMethods,
        IReadOnlyList<string> StringLiterals);
}
