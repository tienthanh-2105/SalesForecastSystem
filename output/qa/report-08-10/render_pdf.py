import pypdfium2 as pdfium
for name in ['07','08']:
 d=pdfium.PdfDocument(f'output/qa/report-08-10/{name}.pdf')
 print(name,len(d))
 for i in range(len(d)): d[i].render(scale=1.4).to_pil().save(f'output/qa/report-08-10/{name}-page-{i+1}.png')
