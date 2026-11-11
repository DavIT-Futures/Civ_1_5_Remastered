import numpy as np
from PIL import Image, ImageDraw, ImageFont
g=np.load('europe_grid.npy'); H,W=g.shape
def t(lon,lat): return int((lon+25)/77*80), int((71.5-lat)/42.5*50)
colours = {'o': (0, 0, 170), 'd': (255, 255, 85), 'p': (170, 85, 0), 'g': (85, 255, 85), 'f': (0, 170, 0),
           'h': (255, 85, 85), 'm': (255, 85, 255), 't': (170, 170, 170), 'a': (255, 255, 255), 's': (0, 170, 170),
           'r': (85, 85, 255)}
starts = [  # leader, civilization (place), lon, lat
 (1,'Rzymianie (Rzym)',12.5,41.9),(9,'Rosjanie (Moskwa)',37.6,55.8),
 (2,'Babilończycy (Babilon)',44.4,32.5),(10,'Zulusi -> Kartagina',10.2,36.8),
 (3,'Niemcy (Berlin)',13.4,52.5),(11,'Francuzi (Paryż)',2.35,48.85),
 (4,'Egipcjanie (Memfis)',31.2,29.9),(12,'Aztekowie -> Hiszpania',-3.7,40.4),
 (5,'Amerykanie -> Irlandia',-6.3,53.3),(13,'Chińczycy -> Wołga',49.1,55.8),
 (6,'Grecy (Ateny)',23.7,38.0),(14,'Anglicy (Londyn)',-0.1,51.5),
 (7,'Hindusi -> Persja',46.0,36.4),(15,'Mongołowie (Saraj)',46.0,48.5),
 (16,'Polacy (Kraków)',19.9,50.1),(17,'Japończycy -> Skandynawia',18.1,59.3),
]
S=14
img=Image.new('RGB',(W*S+330,H*S),(250,250,250)); d=ImageDraw.Draw(img)
for y in range(H):
    for x in range(W):
        d.rectangle([x*S,y*S,x*S+S-1,y*S+S-1],fill=colours[g[y,x]])
f=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',11)
fl=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',13)
fb=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',14)
rep=[]
for n,(leader,name,lon,lat) in enumerate(starts,1):
    x,y=t(lon,lat)
    rep.append((leader,name,x,y,g[y,x]))
    cx,cy=x*S+S//2,y*S+S//2
    d.ellipse([cx-6,cy-6,cx+6,cy+6],fill=(255,255,255),outline=(0,0,0),width=2)
    d.text((cx+8,cy-7),str(n),fill=(0,0,0),font=f,stroke_width=2,stroke_fill=(255,255,255))
lx=W*S+12; ly=10
d.text((lx,ly),'Europa – 80 x 50 pól',fill=(0,0,0),font=fb); ly+=26
for k,lab in [('o','Ocean'),('g','Łąki'),('p','Równiny / step'),('f','Las'),('h','Wzgórza'),('m','Góry'),('t','Tundra'),('s','Bagna'),('d','Pustynia'),('r','Rzeka')]:
    d.rectangle([lx,ly,lx+16,ly+12],fill=colours[k],outline=(0,0,0)); d.text((lx+24,ly-2),lab,fill=(0,0,0),font=fl); ly+=18
ly+=10; d.text((lx,ly),'Starty cywilizacji',fill=(0,0,0),font=fb); ly+=22
for n,(leader,name,x,y,ter) in enumerate(rep,1):
    d.text((lx,ly),f'{n:2}. {name}',fill=(0,0,0),font=fl); ly+=17
d.text((2*S, 30*S+4), 'Nowy\nŚwiat', fill=(255,255,255), font=fb, stroke_width=2, stroke_fill=(0,0,0))
img.save('europe_preview.png')
for r in rep: print(r)
