import importlib.util
from pathlib import Path
import pypdfium2 as pdfium
p=Path('output/qa/report-20261007/final.pdf').resolve()
spec=importlib.util.spec_from_file_location('renderer',r'C:/Users/HP/.codex/plugins/cache/openai-primary-runtime/documents/26.915.20218/skills/documents/render_docx.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
m.convert_to_pdf=lambda *args,**kw:(str(p),'Word COM PDF export')
def convert(pdf,**kw):
 doc=pdfium.PdfDocument(pdf);out=[]
 for i,page in enumerate(doc):
  path=Path(kw['output_folder'])/f'page-raw-{i+1}.png';page.render(scale=kw['dpi']/72).to_pil().save(path);out.append(str(path))
 return out
m.convert_from_path=convert
print(m.rasterize('output/docx/Phạm Minh Tài_Báo cáo ngày 07-10-2026.docx','output/qa/report-20261007/render-v2',120,False,False))


