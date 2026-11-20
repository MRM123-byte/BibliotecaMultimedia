"""Genera el informe con resultados HTTP y capturas reales previamente obtenidas."""
import json, shutil
from pathlib import Path
from xml.sax.saxutils import escape
from reportlab.platypus import SimpleDocTemplate,Paragraph,Spacer,PageBreak,Image,Table,TableStyle,Preformatted
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.pagesizes import A4,landscape
from reportlab.lib import colors
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from PIL import Image as PILImage
from pypdf import PdfReader

root=Path(__file__).resolve().parents[1]
source=root/'artifacts/evidencias'
out=root/'output/pdf/evidencias-api.pdf';out.parent.mkdir(parents=True,exist_ok=True)
report=json.loads((source/'resultados.json').read_text(encoding='utf8'))
pdfmetrics.registerFont(TTFont('Arial',r'C:\Windows\Fonts\arial.ttf'))
pdfmetrics.registerFont(TTFont('ArialBold',r'C:\Windows\Fonts\arialbd.ttf'))
pdfmetrics.registerFontFamily('Arial',normal='Arial',bold='ArialBold')
W,H=landscape(A4);width=W-64
styles={
 'title':ParagraphStyle('title',fontName='ArialBold',fontSize=18,leading=22,spaceAfter=10),
 'body':ParagraphStyle('body',fontName='Arial',fontSize=10,leading=13,spaceAfter=9),
 'small':ParagraphStyle('small',fontName='Arial',fontSize=8,leading=10),
 'code':ParagraphStyle('code',fontName='Courier',fontSize=9,leading=11),
}
story=[]
def p(s,style='body'):story.append(Paragraph(s,styles[style]))
def screenshot(name,maxheight):
    f=source/name
    with PILImage.open(f) as im:w,h=im.size
    scale=min(width/w,maxheight/h)
    story.append(Image(str(f),width=w*scale,height=h*scale,hAlign='LEFT'))
    story.append(Spacer(1,7))
def title(s):p(s,'title')
def new():story.append(PageBreak())
def find(name):return next(t for t in report['pruebas'] if t['prueba']==name)

title('Evidencias de la Web API')
p('Mauricio Rivero Mendez · MRM123-byte · Biblioteca Multimedia API · .NET 10 / SQL Server')
p('Capturas reales de Swagger UI. Los grupos muestran el CRUD principal, las relaciones y las operaciones privadas de biblioteca.')
screenshot('swagger-contenidos.png',235)
screenshot('swagger-biblioteca.png',175)

new();title('Alta, consulta, actualización y borrado')
created=find('Crear contenido y Location')
p(f'<b>{escape(created["operacion"])} - HTTP {created["http"]}</b>')
p('Location: '+escape(created['location']))
story.append(Preformatted(json.dumps(created['respuesta'],ensure_ascii=False,indent=2),styles['code']))
p('Salida real registrada por scripts/probar_api.py. Las credenciales y tokens no se incluyen.','small')
for name in ['Actualizar contenido','Verificar actualización','Consulta de géneros relacionados','Eliminar contenido','Contenido eliminado no existe']:
    t=find(name);p(f'<b>{escape(t["operacion"])} - HTTP {t["http"]}</b>: {escape(name)}.','small')

new();title('Respuestas de error verificadas en Swagger')
p('<b>POST /api/auth/register - HTTP 400.</b> Se intentó registrar nuevamente la cuenta ficticia lector@example.test.')
screenshot('swagger-400.png',225)
p('<b>GET /api/contenidos/2147483647 - HTTP 404.</b> El recurso solicitado no existe.')
screenshot('swagger-404.png',145)
p('También se comprobaron 400 por título vacío, género inexistente, referencia duplicada, estado inválido y puntuación 11; ver el registro de pruebas.','small')

new();title('Persistencia después de reiniciar la aplicación')
r=report['reinicio']
p(f'La prueba automática detuvo el proceso de API con PID <b>{r["pidAntes"]}</b> y arrancó otro con PID <b>{r["pidDespues"]}</b> en el puerto 5199. SQL Server permaneció activo.')
p(f'<b>GET /api/contenidos/{r["contenidoId"]} - HTTP 200.</b> El ID y todos los datos de la ficha coincidieron antes y después del reinicio. También se verificó la entrada de biblioteca.')
p('La captura siguiente es una consulta posterior desde Swagger, puerto 5189, sobre la misma base BibliotecaMultimediaDb.')
screenshot('swagger-persistencia.png',325)
p('Registro de ejecución (UTC): '+escape(report['fechaUtc'])+'. Los PID y respuestas están incluidos en docs/evidencias/resultados.json.','small')

tests=report['pruebas']
for number,rows in enumerate([tests[:23],tests[23:]],1):
    new();title(f'Registro de pruebas HTTP ({number}/2)')
    p(f'{len(tests)} verificaciones aprobadas contra SQL Server. Cada fila identifica la operación y el código HTTP obtenido.')
    data=[[Paragraph(x,styles['small']) for x in ['Prueba','Operación','HTTP']]]
    for t in rows:data.append([Paragraph(escape(t['prueba']),styles['small']),Paragraph(escape(t['operacion']),styles['small']),str(t['http'])])
    table=Table(data,colWidths=[270,width-315,45],repeatRows=1,hAlign='LEFT')
    table.setStyle(TableStyle([('FONTNAME',(0,0),(-1,-1),'Arial'),('FONTSIZE',(0,0),(-1,-1),8),('BACKGROUND',(0,0),(-1,0),colors.HexColor('#dce8f4')),('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.white,colors.HexColor('#f5f7f9')]),('VALIGN',(0,0),(-1,-1),'TOP'),('TOPPADDING',(0,0),(-1,-1),3),('BOTTOMPADDING',(0,0),(-1,-1),3)]))
    story.append(table)

def footer(c,doc):
    c.setFont('Arial',8);c.setFillColor(colors.HexColor('#666666'))
    c.drawString(32,17,'Biblioteca Multimedia API - Evidencias reproducibles')
    c.drawRightString(W-32,17,str(doc.page))
doc=SimpleDocTemplate(str(out),pagesize=(W,H),leftMargin=32,rightMargin=32,topMargin=28,bottomMargin=34,title='Evidencias - Biblioteca Multimedia API',author='Mauricio Rivero Mendez')
doc.build(story,onFirstPage=footer,onLaterPages=footer)
reader=PdfReader(out)
assert len(reader.pages)==6,len(reader.pages)
dest=root/'docs/evidencias';dest.mkdir(parents=True,exist_ok=True)
shutil.copy2(out,root/'docs/Evidencias.pdf')
for f in ['resultados.json','swagger-contenidos.png','swagger-biblioteca.png','swagger-generos.png','swagger-400.png','swagger-404.png','swagger-persistencia.png']:
    shutil.copy2(source/f,dest/f)
print(f'{out}\n6 páginas verificadas; copia incluida en docs/Evidencias.pdf')
