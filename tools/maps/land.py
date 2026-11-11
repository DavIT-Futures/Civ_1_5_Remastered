import sys, numpy as np
sys.path.insert(0, sys.argv[1])
from global_land_mask import globe
from PIL import Image
W,H=80,50
def grid(lon0,lon1,lat0,lat1,merc):
    n=8
    frac=np.zeros((H,W))
    def y2lat(t):
        if not merc: return lat1-(lat1-lat0)*t
        m=lambda a: np.log(np.tan(np.pi/4+np.radians(a)/2))
        v=m(lat1)-(m(lat1)-m(lat0))*t
        return np.degrees(2*np.arctan(np.exp(v))-np.pi/2)
    for y in range(H):
        for x in range(W):
            ts=(y+(np.arange(n)+.5)/n)/H; ss=(x+(np.arange(n)+.5)/n)/W
            lats=y2lat(ts); lons=lon0+(lon1-lon0)*ss
            LA,LO=np.meshgrid(lats,lons)
            frac[y,x]=globe.is_land(LA,LO).mean()
    return frac
lon0,lon1,lat0,lat1,merc=map(float,sys.argv[2:7])
f=grid(lon0,lon1,lat0,lat1,merc>0)
np.save(sys.argv[7],f)
img=Image.fromarray(((f>0.45)*200+30).astype(np.uint8)).resize((640,400),Image.NEAREST)
img.save(sys.argv[7].replace('.npy','.png'))
