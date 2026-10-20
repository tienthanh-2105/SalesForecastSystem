$docPath = (Resolve-Path 'output/docx/Phạm Minh Tài_Báo cáo ngày 09-10-2026.docx').Path
$pdfPath = (Join-Path (Resolve-Path 'output/qa/report-09-10').Path 'preview.pdf')
$word = New-Object -ComObject Word.Application
$word.Visible = $false
try {
  $doc = $word.Documents.Open($docPath, $false, $true)
  $doc.ExportAsFixedFormat($pdfPath, 17)
  $doc.Close($false)
} finally { $word.Quit() }
