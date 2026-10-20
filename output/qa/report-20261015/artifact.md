# Daily report template contract

## Reference and evidence
- Reference DOCX: `E:\Thuc_Tap_BE_Cty_Khang_Nghi\DuAnThucTap\SalesForecastSystem\output\docx\Phạm Minh Tài_Báo cáo ngày 14-10-2026.docx`.
- SHA-256: `7297BC05F3C777300862472CC6CC33AD61F9E5ED9BD1E5E94C87FFDA328E0A6D`; 32,228 bytes; 17 package parts.
- Existing Word render evidence: `output/qa/report-20261014/final-render-3/page-1.png` and `page-2.png`; two pages, both visually inspected. A fresh render_docx.py attempt failed because LibreOffice is unavailable; this does not modify the retained reference.
- One A4 portrait section. Approximate margins from the DOCX section: left 1.18 in, right 0.79 in, top/bottom 0.79 in; header/footer distance 0.49 in.

## Page system and typography
- Preserve section properties and all package parts except intended text in `word/document.xml`.
- Centered, bold, uppercase date title at 18 pt. Body and metadata use 13 pt; section headings use bold 14 pt.
- Metadata paragraphs retain bold labels and regular descriptions. Four summary bullets retain their list formatting and bold lead-ins.
- One Table Grid table, 4 rows by 2 columns. Header row centered and bold; three task categories. Preserve column widths, borders, cell margins, bullet formatting, paragraph indents, and table geometry.
- No header/footer narrative, images, fields, or content controls. Two-page flow: summary and work table begin on page one; table continuation and details on page two.

## Editable slots
- Body paragraph 0: date title.
- Paragraphs 3 and 4: prior-day and current-day summaries. Paragraph 2 project identity stays unchanged.
- Paragraph 6: objective; paragraphs 7–10: four concise summary bullets with bold labels preserved.
- Paragraphs 15–24: detailed work, outcome, verification limits, and next steps. Preserve each bold lead-in run and replace its regular description.
- Table 0 rows 1–3: three category labels and four task bullets per row. Header row remains unchanged.
- Keep the existing 2-page layout and avoid expanding any prose beyond the source template's capacity.

## Fidelity gates
- The reference remains byte-for-byte unchanged.
- The final package may differ only in `word/document.xml`; verify every other package part hash.
- Render final pages, inspect each at full resolution, and check for pagination, clipping, or table split issues. The bundled renderer reported LibreOffice missing. Word export was denied by the local permission prompt, so the final visual pagination gate remains unverified.
