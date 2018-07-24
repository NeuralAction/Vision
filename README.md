# Vision
A eye gaze tracking library based on computer vision and neural network. 

This library is work in progress aggressivly at this point. API structure is not stable now. And this project is personal project for competition. I will open source my model training code after compet.

Current gaze tracking model's mean error is 3.2 cm in 50 cm far without any calibration. With calibration, mean error is ~1.8 cm.

## Demo
![](Web/gazedemo.gif)

- Gaze tracking with calibrations

## TODO
- Gaze tracking calibration codes
- Put more various data into gaze tracking model.

## Main Features
- [x] Single camera gaze tracking.
- [x] Gaze tracking service.
- [x] Abstractions around **OpenCV**
- [x] Abstractions around **Tensorflow**
- [x] Platform abstraction layer (Files, Audio, Video, etc...)

### OpenCV Features
- [x] Face tracking ([Tadas/OpenFace](https://github.com/TadasBaltrusaitis/OpenFace))
- [x] Cascade object detection
- [x] Some examples of openCV
- [x] Cross platform webcam I/O

### Tensorflow Features
- [x] Data sharing between OpenCV
- [x] Input image normalization
- [x] GPU acceleration supports
- [x] Model imports
