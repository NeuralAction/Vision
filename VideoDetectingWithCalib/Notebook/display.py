import cv2
import numpy as np
import IPython.display as disp
import matplotlib.pyplot as plt

def clear(wait=True):
    disp.clear_output(wait=wait)
    return

def imshow(img, maxSize=(640,480), format='jpg', compressPercent=0.9):
    imgw = float(img.shape[1])
    imgh = float(img.shape[0])
    if imgw > maxSize[0] or imgh > maxSize[1]:
        scale = min(maxSize[0]/imgw, maxSize[1]/imgh)
        img = cv2.resize(img, dsize=(0,0), fx=scale, fy=scale, interpolation=cv2.INTER_LINEAR)
    encode_param = [int(cv2.IMWRITE_JPEG_QUALITY), int(compressPercent*100)]
    ret, png = cv2.imencode('.'+format, img, encode_param)
    decoded = disp.Image(data=png, format=format)
    disp.display(decoded)
    return

def vidshow(img, maxSize=(320,240), format='jpg', compressPercent=0.6, clear=True):
    if clear:
        disp.clear_output(wait=True)
    imshow(img, maxSize, format, compressPercent)
    return

def pltshow(img):
    plt.imshow(img)
    plt.show()
    return


