# Daily report template contract
Reference: E:\Thuc_Tap_BE_Cty_Khang_Nghi\DuAnThucTap\SalesForecastSystem\output\docx\Phạm Minh Tài_Báo cáo ngày 02-10-2026.docx; SHA256 9aff97f81cec19a5cb17eb611bf5df9c22cb2e48f300e8fc1e3d86f2de9fbda0. Three reference page PNGs in output/qa/report-20261002/final-1.png through final-3.png inspected.
Preserve the reference package except document.xml. A4 portrait section, margins, Times New Roman, title, table, list styles and all paragraph/run formatting unchanged.
Slots: paragraph 0 date; 3 previous work; 4 today; 6 purpose; 7–10 objectives; 15–29 three table groups; 32–41 detail groups. Preserve paragraphs 2, 12–14, 31 and blank slots.
Coverage: frontend Excel API integration, profit and staff group reports, 29 unit tests, 17 mocked browser tests, actual startup and HTTP 200. Do not claim live database business integration testing.
Renderer fallback: packaged render_docx.py cannot find soffice.exe. Use Word COM read-only PDF export and invoke packaged rasterize with the exported PDF, without saving reference or target from Word.
Fidelity deviation: remove forced pageBreakBefore at paragraph 31 to avoid a nearly empty page. Preserve all fonts, table geometry and remaining formatting. PDFium used for rasterization because bundled Poppler executable is absent.
