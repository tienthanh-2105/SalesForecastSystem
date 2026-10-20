# Daily report template contract

## Reference and evidence
- Reference: `E:\Thuc_Tap_BE_Cty_Khang_Nghi\DuAnThucTap\SalesForecastSystem\output\docx\Phạm Minh Tài_Báo cáo ngày 12-10-2026.docx`
- SHA-256: `5426c5d2be6ab1e27130da229345bdc311c8ef5cd488f17907ff580a6ffa7d5a`; size: 29094 bytes.
- Rendered by Microsoft Word to PDF, then rasterized with Poppler because bundled LibreOffice was unavailable. Reference render: `reference-render/page-1.png`, `reference-render/page-2.png`; exactly 2 pages.
- Section/style evidence: `reference-style-evidence.json`; section audit reports one A4 portrait section.
- Package part sizes and hashes: `reference-package-inventory.json`.
- The reference was rendered and both pages visually inspected. It remains unchanged.

## Page system
- One section, A4 portrait (8.268 x 11.693 in); margins L 1.181 in, R 0.788 in, top/bottom 0.788 in; header/footer distances 0.492 in.
- No distinct first-page header; header/footer linked. Preserve section properties and all document parts except intended text in `word/document.xml`.
- Two-page flow: page 1 includes title, project/date summary, goal bullets, and the first daily-work table rows; page 2 continues the same table then carries detailed work.

## Typography and components
- Title: centered, bold, 18 pt, uppercase date heading; direct formatted Normal paragraph.
- Metadata and body: 13 pt; bold lead-in labels (`Dự án:`, `Hôm trước:`, `Hôm nay:`, `Mục tiêu:`) followed by regular text. Existing bullet indent/numbering and paragraph spacing are retained.
- Section headings: bold 14 pt (`1. Công việc trong ngày`, `2. Chi tiết thực hiện`). They are direct-formatted Normal paragraphs, not heading styles.
- Summary list: four existing bullet paragraphs, 13 pt, bold labels followed by regular descriptions.
- Daily-work table: existing Table Grid, 4 rows x 2 columns; approximate widths 2.166 in and 4.134 in. Header row centered and bold. Three work-category rows with bullets in the second cell. Preserve table geometry, borders, cell margins, and existing paragraph/list formatting.
- No header/footer content, figures, text boxes, content controls, or fields are used for the report narrative.

## Slot map and use
- `/word/document.xml` body paragraph 0: rewrite report date title.
- Paragraphs 2-4: preserve project name; rewrite prior-day and current-day summary.
- Paragraph 6: rewrite the day's objective.
- Paragraphs 7-10: rewrite four concise scope bullets; preserve bold labels and bullet formatting.
- Paragraphs 12 and 14: preserve section headings.
- Paragraphs 15-24: rewrite daily details/results; preserve run-level bold lead-ins, list numbering, indents, and paragraph formats. Match the existing paragraph/run shape by retaining bold label runs and replacing only text values; use empty existing trailing runs as needed.
- Table 0 row 0: preserve the two header cells.
- Table 0 rows 1-3: replace the three category names and work bullet text for today's tasks; keep 4x2 geometry and paragraph formatting. Retain three concise categories to match page flow.
- All styles, numbering, theme, section properties, relationships, custom XML, headers, footers, and package parts outside `word/document.xml` are preserve-only.

## Fidelity gates
- Change only intended text inside `word/document.xml`; package part inventory/hash comparison must show every other part byte-for-byte unchanged.
- Preserve A4 portrait, margins, title/body hierarchy, table grid and two-page pagination. Render final DOCX, inspect every page, and check for table splits, clipping, and unintended pagination.
- Keep this reference byte-for-byte unchanged.

- Pagination repair: remove only trailing empty body paragraphs immediately before final section properties when they create a blank final page; preserve the section properties.
