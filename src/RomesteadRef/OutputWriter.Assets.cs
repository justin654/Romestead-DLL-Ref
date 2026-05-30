using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private static string BuildSearchScript() =>
        """
        document.addEventListener("DOMContentLoaded", () => {
          const entries = window.ROMESTEAD_REF_SEARCH_INDEX || [];
          const input = document.getElementById("searchBox");
          const results = document.getElementById("searchResults");
          const kindFilters = Array.from(document.querySelectorAll(".search-kind"));
          const publicOnly = document.getElementById("searchPublicOnly");
          const escapeHtml = value => String(value ?? "").replace(/[&<>"']/g, character => ({
            "&": "&amp;",
            "<": "&lt;",
            ">": "&gt;",
            "\"": "&quot;",
            "'": "&#39;"
          }[character]));
          const render = () => {
            const query = (input.value || "").trim().toLowerCase();
            const enabledKinds = new Set(kindFilters.filter(filter => filter.checked).map(filter => filter.value));
            const onlyPublic = publicOnly?.checked ?? false;
            if (!query) {
              results.innerHTML = "";
              return;
            }
            const tokens = query.split(/\s+/).filter(Boolean);
            const matches = entries
              .filter(entry => enabledKinds.has(entry.kind))
              .filter(entry => !onlyPublic || entry.visibility === "public" || entry.visibility === undefined)
              .filter(entry => tokens.every(token => (entry.search || "").toLowerCase().includes(token)))
              .slice(0, 120);
            if (matches.length === 0) {
              results.innerHTML = "<p class=\"muted\">No matches.</p>";
              return;
            }
            results.innerHTML = matches.map(entry =>
              `<a class="result" href="${escapeHtml(entry.link)}"><strong>${escapeHtml(entry.kind)}</strong><span>${escapeHtml(entry.title)}</span><small>${escapeHtml(entry.subtitle)}</small></a>`
            ).join("");
          };
          input.addEventListener("input", render);
          for (const filter of kindFilters) {
            filter.addEventListener("change", render);
          }
          publicOnly?.addEventListener("change", render);
          document.addEventListener("keydown", event => {
            if (event.key !== "/" || event.ctrlKey || event.metaKey || event.altKey) {
              return;
            }
            const tagName = document.activeElement?.tagName;
            if (tagName === "INPUT" || tagName === "TEXTAREA" || document.activeElement?.isContentEditable) {
              return;
            }
            event.preventDefault();
            input.focus();
          });
        });
        """;

    private static string BuildStyleSheet() =>
        """
        :root {
          color-scheme: light;
          --bg: #f6f8fa;
          --panel: #ffffff;
          --panel-2: #f1f3f5;
          --panel-3: #f6f8fa;
          --ink: #24292f;
          --muted: #57606a;
          --accent: #0969da;
          --line: #d0d7de;
          --soft-line: #eaeef2;
          --code-bg: #f6f8fa;
          --success: #1a7f37;
          --danger: #cf222e;
        }

        * { box-sizing: border-box; }
        html, body {
          overflow-x: hidden;
        }
        body {
          margin: 0;
          font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", "Noto Sans", Helvetica, Arial, sans-serif;
          background: var(--bg);
          color: var(--ink);
          line-height: 1.45;
          font-size: 14px;
        }

        a { color: var(--accent); text-decoration: none; }
        a:hover { text-decoration: underline; }
        .site-frame {
          min-height: 100vh;
        }
        .topbar {
          position: sticky;
          top: 0;
          z-index: 5;
          display: flex;
          align-items: center;
          justify-content: space-between;
          gap: 18px;
          min-height: 54px;
          padding: 0 max(16px, calc((100vw - 1320px) / 2));
          background: rgba(255, 255, 255, 0.96);
          border-bottom: 1px solid var(--line);
          backdrop-filter: blur(8px);
        }
        .brand {
          color: var(--ink);
          font-weight: 650;
          letter-spacing: -0.01em;
          white-space: nowrap;
        }
        .brand:hover {
          text-decoration: none;
        }
        .topnav {
          display: flex;
          gap: 4px;
          align-items: center;
          overflow-x: auto;
        }
        .topnav a {
          color: var(--muted);
          padding: 7px 9px;
          border-radius: 6px;
          white-space: nowrap;
        }
        .topnav a:hover,
        .topnav a.active {
          color: var(--ink);
          background: var(--panel-3);
          text-decoration: none;
        }
        code, a, p, td, th, li {
          min-width: 0;
        }
        code {
          font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, "Liberation Mono", monospace;
          font-size: 0.92em;
          background: var(--code-bg);
          border-radius: 4px;
          padding: 0.12em 0.28em;
          overflow-wrap: anywhere;
        }
        .page {
          width: min(1320px, calc(100vw - 32px));
          margin: 0 auto;
          padding: 20px 0 48px;
        }
        .site-header, .panel {
          background: var(--panel);
          border: 1px solid var(--line);
          border-radius: 6px;
        }
        .site-header {
          padding: 18px 20px;
          display: flex;
          justify-content: space-between;
          align-items: flex-start;
          gap: 20px;
        }
        h1, h2, h3 { margin: 0 0 14px; }
        h1 {
          font-size: 1.55rem;
          font-weight: 600;
          letter-spacing: 0;
        }
        h2 {
          font-size: 1.12rem;
          font-weight: 600;
        }
        h3 {
          font-size: 1rem;
          font-weight: 600;
        }
        .lede { margin: 0; color: var(--muted); }
        .breadcrumb {
          margin: 0 0 12px;
          color: var(--muted);
          font-size: 0.95rem;
        }
        .header-actions {
          display: flex;
          gap: 8px;
          flex-wrap: wrap;
          justify-content: flex-end;
        }
        .button {
          display: inline-block;
          padding: 6px 10px;
          border-radius: 6px;
          background: var(--panel);
          border: 1px solid var(--line);
          color: var(--ink);
          text-decoration: none;
          font-size: 0.9rem;
        }
        .button:hover {
          background: var(--panel-3);
          text-decoration: none;
        }
        .primary-button {
          color: #ffffff;
          background: var(--ink);
          border-color: var(--ink);
        }
        .primary-button:hover {
          color: #ffffff;
          background: #3b434c;
          border-color: #3b434c;
        }
        .notice {
          margin: 12px 0 0;
          color: var(--muted);
          max-width: 86ch;
        }
        kbd {
          font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, "Liberation Mono", monospace;
          font-size: 0.82em;
          border: 1px solid var(--line);
          border-bottom-color: #afb8c1;
          border-radius: 4px;
          background: var(--panel-3);
          padding: 1px 5px;
          color: var(--ink);
        }
        .member-summary, .metrics {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
          gap: 10px;
        }
        .title-row, .method-heading, .member-name-cell {
          display: flex;
          align-items: center;
          gap: 8px;
          flex-wrap: wrap;
          min-width: 0;
        }
        .title-row h1,
        .method-heading h3 {
          margin-bottom: 0;
        }
        .copy-button {
          border: 1px solid var(--line);
          background: var(--panel);
          color: var(--ink);
          border-radius: 6px;
          padding: 4px 8px;
          font: inherit;
          font-size: 0.86rem;
          cursor: pointer;
        }
        .copy-button:hover {
          background: var(--panel-3);
        }
        .copy-button.copied {
          color: var(--success);
          border-color: rgba(26, 127, 55, 0.35);
        }
        .permalink {
          color: var(--muted);
          font-weight: 600;
        }
        .change-badge {
          display: inline-flex;
          align-items: center;
          border: 1px solid var(--line);
          border-radius: 999px;
          padding: 1px 7px;
          font-size: 0.78rem;
          line-height: 1.4;
          color: var(--muted);
          background: var(--panel-3);
        }
        .change-added {
          color: var(--success);
          border-color: rgba(26, 127, 55, 0.35);
          background: #dafbe1;
        }
        .change-removed {
          color: var(--danger);
          border-color: rgba(207, 34, 46, 0.35);
          background: #ffebe9;
        }
        .change-changed {
          color: #9a6700;
          border-color: rgba(154, 103, 0, 0.35);
          background: #fff8c5;
        }
        .metric {
          background: var(--panel-3);
          border: 1px solid var(--soft-line);
          border-radius: 6px;
          padding: 12px;
        }
        .metric strong {
          display: block;
          font-size: 1.18rem;
          margin-top: 4px;
        }
        .search-panel {
          margin: 12px 0;
          display: grid;
          gap: 8px;
          background: var(--panel);
          border: 1px solid var(--line);
          border-radius: 6px;
          padding: 16px;
        }
        .search-panel label:first-child {
          font-weight: 600;
        }
        .search-panel input[type="search"] {
          width: 100%;
          padding: 11px 12px;
          border-radius: 6px;
          border: 1px solid var(--line);
          font-size: 1rem;
          background: var(--panel);
        }
        .search-panel input:focus,
        .member-toolbar input[type="search"]:focus,
        .member-toolbar select:focus {
          outline: 2px solid rgba(9, 105, 218, 0.18);
          border-color: var(--accent);
        }
        .search-results {
          display: grid;
          gap: 6px;
        }
        .search-filters {
          display: flex;
          flex-wrap: wrap;
          gap: 10px 14px;
          color: var(--muted);
          font-size: 0.92rem;
        }
        .search-filters label {
          display: inline-flex;
          align-items: center;
          gap: 5px;
        }
        .search-help {
          margin: 0;
          color: var(--muted);
          font-size: 0.92rem;
        }
        .result {
          display: grid;
          gap: 3px;
          padding: 10px 12px;
          border-radius: 6px;
          border: 1px solid var(--line);
          background: var(--panel);
        }
        .result:hover {
          background: var(--panel-3);
          text-decoration: none;
        }
        .result strong {
          font-size: 0.78rem;
          color: var(--muted);
        }
        .result small {
          color: var(--muted);
          font-size: 0.9rem;
        }
        .panel {
          padding: 16px;
          margin-top: 12px;
        }
        .compact-panel {
          padding: 14px 18px;
        }
        .assembly-grid, .topic-grid, .surface-grid, .surface-overview, .opportunity-grid {
          display: grid;
          gap: 10px;
        }
        .assembly-grid, .topic-grid, .surface-grid {
          grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
          margin-top: 10px;
        }
        .assembly-card, .topic-card, .surface-card, .overview-card, .opportunity-card, .candidate {
          display: block;
          padding: 12px 14px;
          border-radius: 6px;
          border: 1px solid var(--soft-line);
          background: var(--panel);
        }
        .assembly-card:hover, .topic-card:hover {
          border-color: var(--line);
          background: var(--panel-3);
        }
        .assembly-card, .topic-card, .surface-card, .overview-card, .opportunity-card, .candidate, .result, .panel {
          min-width: 0;
        }
        .assembly-card p, .topic-card p {
          margin: 8px 0 0;
          overflow-wrap: anywhere;
        }
        .topic-card .muted, .assembly-card .muted {
          display: -webkit-box;
          -webkit-line-clamp: 3;
          -webkit-box-orient: vertical;
          overflow: hidden;
        }
        .surface-card {
          text-decoration: none;
          color: inherit;
        }
        .surface-card strong,
        .surface-card span,
        .surface-card small {
          display: block;
        }
        .surface-card span {
          margin-top: 8px;
        }
        .surface-card small {
          margin-top: 8px;
          color: var(--muted);
        }
        .surface-overview {
          grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
          margin: 14px 0;
        }
        .docs-panel p {
          margin: 0 0 10px;
          max-width: 88ch;
        }
        .docs-grid,
        .guide-grid {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
          gap: 12px;
        }
        .docs-grid article,
        .guide-grid article {
          min-width: 0;
          border: 1px solid var(--soft-line);
          border-radius: 6px;
          padding: 12px;
          background: var(--panel);
        }
        .docs-grid article p,
        .guide-grid article p {
          margin: 0;
        }
        .numbered-list {
          margin: 12px 0 0;
          padding-left: 24px;
        }
        .numbered-list li {
          margin: 10px 0;
          padding-left: 4px;
        }
        .relationship-grid {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
          gap: 12px;
        }
        .relationship-grid article {
          min-width: 0;
          border: 1px solid var(--soft-line);
          border-radius: 6px;
          padding: 12px;
          background: var(--panel);
        }
        .overview-card h3,
        .opportunity-card h4 {
          margin-bottom: 10px;
        }
        .opportunity-grid {
          grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
          margin-top: 10px;
        }
        .opportunity-card > .candidate {
          margin-top: 10px;
          background: var(--panel-3);
        }
        .section-details {
          margin-top: 14px;
          padding-top: 10px;
          border-top: 1px solid var(--line);
        }
        .section-details summary {
          padding: 6px 0;
        }
        .assembly, .namespace {
          border-top: 1px solid var(--line);
          padding-top: 12px;
          margin-top: 12px;
        }
        .assembly:first-of-type, .namespace:first-of-type {
          border-top: 0;
          padding-top: 0;
          margin-top: 0;
        }
        summary {
          cursor: pointer;
          display: flex;
          justify-content: space-between;
          gap: 16px;
          font-weight: 600;
          align-items: center;
          list-style: none;
          border-radius: 6px;
          padding: 6px 8px;
          margin: -6px -8px;
        }
        summary:hover {
          background: var(--panel-3);
        }
        summary::-webkit-details-marker {
          display: none;
        }
        summary::before {
          content: ">";
          flex: 0 0 auto;
          color: var(--muted);
          font-size: 1.1rem;
          line-height: 1;
          transform: rotate(0deg);
          transition: transform 120ms ease;
        }
        details[open] > summary::before {
          transform: rotate(90deg);
        }
        summary span {
          color: var(--muted);
          font-weight: 400;
        }
        .type-list, .list {
          list-style: none;
          padding: 0;
          margin: 12px 0 0;
          display: grid;
          gap: 8px;
        }
        .type-list li, .list li {
          display: flex;
          justify-content: space-between;
          gap: 12px;
          align-items: baseline;
          padding: 8px 0;
          border-bottom: 1px solid var(--soft-line);
        }
        .type-list li:last-child, .list li:last-child {
          border-bottom: 0;
        }
        .type-list a, .list a, .type-list span, .list span {
          min-width: 0;
          overflow-wrap: anywhere;
        }
        .section-heading {
          display: flex;
          justify-content: space-between;
          gap: 12px;
          align-items: baseline;
          margin-bottom: 8px;
        }
        .section-heading h2 {
          margin-bottom: 0;
        }
        .section-heading span {
          color: var(--muted);
        }
        .reference-table, table {
          width: 100%;
          border-collapse: collapse;
          table-layout: fixed;
        }
        th, td {
          text-align: left;
          padding: 9px 10px;
          border-bottom: 1px solid var(--soft-line);
          vertical-align: top;
          overflow-wrap: anywhere;
        }
        th {
          color: var(--muted);
          font-weight: 600;
          background: var(--panel-3);
        }
        tr:hover td {
          background: #fcfcfd;
        }
        .diff-table {
          table-layout: auto;
        }
        .diff-table th:nth-child(1),
        .diff-table td:nth-child(1) {
          width: 86px;
          white-space: nowrap;
        }
        .diff-table th:nth-child(2),
        .diff-table td:nth-child(2) {
          width: 150px;
          white-space: nowrap;
        }
        .diff-table th:nth-child(5),
        .diff-table td:nth-child(5) {
          width: 150px;
          white-space: nowrap;
        }
        .diff-table th:nth-child(3),
        .diff-table td:nth-child(3) {
          width: 28%;
        }
        .diff-table td:nth-child(4) {
          min-width: 360px;
        }
        .diff-table code {
          display: inline;
          background: transparent;
          padding: 0;
        }
        .diff-kind {
          font-weight: 600;
        }
        .method {
          padding: 14px 0;
          border-top: 1px solid var(--soft-line);
        }
        .method h3 {
          margin-bottom: 8px;
        }
        .method:first-of-type {
          border-top: 0;
          padding-top: 0;
        }
        .compact li {
          padding: 4px 0;
        }
        .muted, .footer {
          color: var(--muted);
        }
        .debug-details {
          margin-top: 10px;
        }
        .debug-details summary {
          justify-content: flex-start;
          width: fit-content;
          color: var(--accent);
          font-weight: 600;
        }
        .method > details {
          margin-top: 8px;
          border: 1px solid var(--soft-line);
          border-radius: 6px;
          padding: 10px;
          background: var(--panel-3);
        }
        .method > details > summary {
          color: var(--accent);
          justify-content: flex-start;
          margin: -10px;
          padding: 10px;
          width: calc(100% + 20px);
        }
        .namespace > summary,
        .assembly > summary,
        .section-details > summary {
          justify-content: flex-start;
        }
        .namespace > summary span,
        .assembly > summary span,
        .section-details > summary span {
          margin-left: auto;
        }
        .member-tools {
          position: sticky;
          top: 0;
          z-index: 2;
        }
        .member-toolbar {
          display: grid;
          grid-template-columns: minmax(180px, 1fr) auto auto auto;
          gap: 10px;
          align-items: center;
        }
        .member-toolbar > label:first-child {
          grid-column: 1 / -1;
          color: var(--muted);
          font-size: 0.95rem;
        }
        .member-toolbar input[type="search"], .member-toolbar select {
          width: 100%;
          padding: 9px 10px;
          border-radius: 6px;
          border: 1px solid var(--line);
          background: var(--panel);
          color: var(--ink);
        }
        .stat-list {
          display: flex;
          gap: 16px;
          flex-wrap: wrap;
          list-style: none;
          margin: 0;
          padding: 0;
        }
        .stat-list li {
          display: flex;
          gap: 8px;
          align-items: baseline;
        }
        .stat-list span {
          color: var(--muted);
        }
        .footer {
          margin-top: 24px;
          font-size: 0.9rem;
        }
        .site-footer {
          display: flex;
          gap: 12px;
          flex-wrap: wrap;
          align-items: center;
          border-top: 1px solid var(--line);
          padding-top: 16px;
        }
        .site-footer span {
          margin-right: auto;
        }
        @media (max-width: 860px) {
          .page {
            width: min(100vw - 20px, 1280px);
            padding-top: 12px;
          }
          .site-header {
            flex-direction: column;
            align-items: stretch;
          }
          .header-actions {
            justify-content: flex-start;
          }
          .member-toolbar {
            grid-template-columns: 1fr;
          }
          summary, .type-list li, .list li {
            flex-direction: column;
            align-items: flex-start;
          }
          .section-heading {
            flex-direction: column;
            align-items: flex-start;
          }
        }
        """;
}
