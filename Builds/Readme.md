# Goal

Utilize OpenCvSharp inside Unity3D to recogize a face.

# Usage

1. [Setup training data set](#dataset-setup).
2. Ensure webcam is connected.
3. Run executable.
    1. After the application loads, you should see the webcam data stream.
    2. If a face is detected, it will show the detection with a bounding box around that face.
    3. If that face matches the dataset, it is recognized. it will display `Face #1 ??` above the top left of its bounding box. The second number, ?? in this case, is the confidence value. Lower confidence is a stronger match.
    4. What you see in the webcam feed, including the bounding box and recognization label, will be exported as images. These images are put in the same folder where you setup the training data. They are marked with a session ID. The session ID is a large integer. An example folder would be \FaceRecognition_Data\StreamingAssets\637828609* . Inside that session ID folder is every image captured in that session. Each image will be named with an ID similiar to the session ID.
4. Exit the app by pressing the Escape key on your keyboard.

# DataSet

Importing a dataset is required for face recognition. It trains the model what type of face to recognize. One face can be recognized at a time. To recongize a different face, the app should be exited and its dataset replaced.

### Dataset setup

1. Consists of multiple *.jpg images of a single face. Preferably cropped to be only the face.
    1. You can run the projects source code in the Unity Editor to set it up to generate cropped faces using the `IsRecordOnlyFace` option.
2. Images must be placed in \FaceRecognition_Data\StreamingAssets\ in a folder that starts with "TrainFace" .
    1. The last part of the folder name will modify the label shown on the bounding box. For example, placing images in \FaceRecognition_Data\StreamingAssets\TrainFaceFreelancer\* will label recognized faces as `Freelancer`.

# Source code

To use the source code.
1. Unzip it to a folder.
2. Open that folder as a new project with Unity 2021.2.6
3. Open the main scene, \Assets\_FaceRecognition\FaceRecognition.unity
4. Play the main scene in the Unity Editor and you will have the same [Usage experience](#usage) as the executable.

### Scene setup

The Unity Editor has multiple panels. Below is how the FaceRecognition.unity scene uses those panels.

1. In the the __Console__ panel - you'll see a stream of numbers whenever any face is detected in playmode. The lower the number in parentheses, the closer it is to matching the faces held in \StreamingAssets\TrainFace*\ . When a number is less than 80 is is considered recognized.
2. In the __Hierarchy__ panel - you'll see 3 game objects.
    1. Main Camera - controls how zoomed in the face is
    2. KeyboardInput - Handles exiting the app when Escape is pressed on a keyboard
    3. WebCam - Shows the webcam data with a face recognition overlay
3. If the WebCam is selected, in the __Inspector__ panel - you'll see multiple components. The important one is at the bottom, called Face Recognition. FaceRecognition.cs can be modified to change recognition parameters, such as threshold where faces are considered recognized. In the inspector, it has two options
    1. IsRecording - Must be checked for any data to be exported. Otherwise leave unchecked to avoid filling up your hard drive.
    2. IsRecordOnlyFace - If IsRecording is also checked, exports only the bounding box of a face. Using this option is one way to build a training set of faces that can be copied into the executables \FaceRecognition_Data\StreamingAssets\TrainFace*\*
