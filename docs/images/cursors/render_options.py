"""Original polygon artwork. SVG and supersampled PNG share these exact vertices."""
from pathlib import Path
from datetime import datetime, timezone
from PIL import Image, ImageDraw, ImageFont
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[3]
DEST = ROOT / 'Assets/Art/Cursors'
HERE = Path(__file__).resolve().parent
S = 4
ART = [
 ('A', 'Ada Pusulası', 'AdaPusulasi', 'Fildişi yüzey · turkuaz kenar · kiremit pusula', [
  ('#193E47', [(6,5),(50,35),(32,39),(41,54),(30,60),(23,44),(12,54)]),
  ('#F2E7CD', [(9,11),(43,33),(28,36),(36,52),(31,55),(23,38),(15,47)]),
  ('#B2CDBD', [(9,11),(26,33),(23,38),(15,47)]),
  ('#3E8B88', [(9,11),(15,47),(14,50),(8,15)]),
  ('#C96F4B', [(27,24),(32,29),(27,34),(23,29)])]),
 ('B', 'Kıyı Kristali', 'KiyiKristali', 'İnce asimetrik siluet · açık su ve kum fasetleri', [
  ('#173B49', [(6,5),(47,36),(32,38),(40,55),(29,60),(22,43),(14,52)]),
  ('#69C7C6', [(9,11),(40,34),(27,35),(35,53),(30,55),(22,37),(16,45)]),
  ('#B5E6D9', [(9,11),(27,35),(22,37),(16,45)]),
  ('#318D9E', [(9,11),(40,34),(27,35)]),
  ('#E4CEA3', [(27,35),(35,53),(30,55),(22,37)])]),
 ('C', 'Fener Oku', 'FenerOku', 'Lacivert çerçeve · sıcak ışık kaması · sade uç', [
  ('#182E42', [(6,5),(53,34),(34,39),(43,54),(30,61),(22,44),(12,54)]),
  ('#EFE6CF', [(9,11),(45,33),(29,37),(37,52),(31,56),(22,38),(15,47)]),
  ('#D7AA59', [(9,11),(29,37),(37,52),(31,56),(22,38)]),
  ('#F5D79A', [(9,11),(45,33),(29,37)]),
  ('#748D94', [(9,11),(15,47),(14,50),(8,16)])])
]

def artwork(polys, size=64):
    image=Image.new('RGBA',(size*S,size*S))
    draw=ImageDraw.Draw(image)
    for color, points in polys:
        draw.polygon([(round(x*size*S/64),round(y*size*S/64)) for x,y in points],fill=color)
    return image.resize((size,size),Image.Resampling.LANCZOS)

def polygon_svg(polys):
    return ''.join('<polygon fill="%s" points="%s"/>' % (color, ' '.join(f'{x},{y}' for x,y in points)) for color,points in polys)

DEST.mkdir(parents=True,exist_ok=True)
for key,title,filename,subtitle,polys in ART:
    (DEST/f'{filename}.svg').write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 64 64"><title>{escape(title)}</title><desc>Original low-poly island pointer. Hotspot: 6,5. Transparent background.</desc>{polygon_svg(polys)}</svg>\n')
    artwork(polys).save(DEST/f'{filename}.png')

W,H=1380,860
panel=Image.new('RGB',(W*S,H*S),'#F3EFE5');d=ImageDraw.Draw(panel)
svg=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}"><rect width="{W}" height="{H}" fill="#F3EFE5"/>']
font='/System/Library/Fonts/Supplemental/Arial.ttf';bold='/System/Library/Fonts/Supplemental/Arial Bold.ttf'
def text(x,y,value,size=20,color='#243F48',heavy=False):
    d.text((x*S,y*S),value,font=ImageFont.truetype(bold if heavy else font,size*S),fill=color)
    svg.append(f'<text x="{x}" y="{y+size*.9}" font-family="Arial, sans-serif" font-size="{size}" fill="{color}"'+(' font-weight="700"' if heavy else '')+f'>{escape(value)}</text>')
def rect(x,y,w,h,color,r=0):
    d.rounded_rectangle((x*S,y*S,(x+w)*S,(y+h)*S),radius=r*S,fill=color)
    svg.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{color}"/>')
def cursor(x,y,size,polys):
    art=artwork(polys,size*S)
    panel.paste(art,(x*S,y*S),art)
    svg.append(f'<g transform="translate({x} {y}) scale({size/64})">{polygon_svg(polys)}</g>')

text(48,32,'ADANIN İMLECİ',17,'#777E73',True)
text(48,64,'Şehrin diliyle çizilmiş üç seçenek',36,heavy=True)
text(48,116,'Keskin sol üst uç · özgün vektör çizim · gündüz ve gece okunabilirlik',19,'#64756E')
for index,(key,title,filename,subtitle,polys) in enumerate(ART):
    x=48+index*444
    rect(x,174,414,600,'#FFFFFF',20)
    text(x+24,194,key,22,'#B36B49',True);text(x+60,190,title,27,heavy=True)
    text(x+24,239,subtitle.split(' · ')[0],17,'#68796F')
    rect(x+20,283,374,205,'#AEBF8D',12)
    text(x+38,299,'GÜNDÜZ',13,'#3F574B',True)
    cursor(x+137,319,150,polys)
    cursor(x+47,400,32,polys)
    text(x+88,412,'32 px',14,'#3F574B')
    rect(x+20,505,374,205,'#173347',12)
    text(x+38,521,'GECE',13,'#A4C4CD',True)
    cursor(x+137,541,150,polys)
    cursor(x+47,622,32,polys)
    text(x+88,634,'32 px',14,'#A4C4CD')
    text(x+24,731,'64 × 64 PNG + SVG  /  uç: (6, 5)',15,'#68796F')
text(48,803,'Büyük çizimler ayrıntı önizlemesidir. Küçük örnekler gerçek 32 px boyutundadır.',17,'#68796F')
svg.append('</svg>')
(HERE/'CursorOptions.svg').write_text('\n'.join(svg)+'\n')
panel.resize((W,H),Image.Resampling.LANCZOS).save(HERE/'CursorOptions.png')
(HERE/'README.md').write_text('Three original low-poly cursor concepts. No runtime cursor is activated.\n\nAll source SVGs and transparent PNGs: `Assets/Art/Cursors/`. Every icon is exactly 64 × 64 with the functional top-left tip / hotspot at **(6, 5)**. Panel contains enlarged previews and native 32 px previews on day/night colours. Rebuild with this folder’s `render_options.py` using Pillow.\n\nCreated: '+datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M:%S UTC')+'\n')
print(HERE/'CursorOptions.png')
