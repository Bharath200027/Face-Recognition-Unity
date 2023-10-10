using OpenCvSharp;
using OpenCvSharp.Face;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Rect = OpenCvSharp.Rect;

public class FaceRecognition : MonoBehaviour
{
    #region [ Fields & Properties ]
    [Tooltip("Must be true for images to be exported")]
    public bool IsRecording = false;

    [Tooltip("When true, use this for building training datasets the image is only the face rectangle. When false, images are the entire webcam view with face rectangles and labels")]
    public bool IsRecordOnlyFace = false;

    // session info
    private const string trainFace = "TrainFace";
    private readonly string sessionId = System.DateTime.Now.Ticks.ToString();
    private string sessionFolder;
    private string faceLabel = "Face #1";

    // OpenCv parameters
    private readonly double maxRecognizationConfidence = 80; // How much confidence to have in predicting a match. Lower numbers are more confidence.
    private WebCamTexture webCamTexture;
    private CascadeClassifier cascade;
    private Rect firstFace;

    /// <summary> Face Recognization model </summary>
    private LBPHFaceRecognizer recognizer
    {
        get
        {
            if (_recognizer == null)
            {
                faceLabel = parseFaceLabel(trainFace, out string folderPath);
                _recognizer = trainModelOnFolder(folderPath, 1);
            }
            return _recognizer;
        }
    }
    private LBPHFaceRecognizer _recognizer;

    /// <summary> cached webcam material </summary>
    public Material webCamMat
    {
        get
        {
            if (_webCamMat == null)
            {
                _webCamMat = GetComponent<Renderer>().material;
            }
            return _webCamMat;
        }
    }
    private Material _webCamMat;
    #endregion [ Fields & Properties ]

    #region [ Unity Lifecycle ]
    void Start()
    {
        // setup webcam feed
        webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name);
        webCamTexture.Play();

        // initialize OpenCv
        cascade = new CascadeClassifier(Application.streamingAssetsPath + @"/haarcascade_frontalface_default.xml");

        // setup export of webcam frames as images
        if (IsRecording)
        {
            // create folder for files exported this session
            sessionFolder = Path.Join(Application.streamingAssetsPath, sessionId);
            Directory.CreateDirectory(sessionFolder);
        }
    }

    void Update()
    {
        // detection
        Mat frame = runFaceDetection();

        // recognization
        var frameWithFaceRectangles = addFaceRectangle(frame);
        webCamMat.mainTexture = frameWithFaceRectangles;

        // export images
        if (IsRecording)
        {
            exportImage(IsRecordOnlyFace
                ? OpenCvSharp.Unity.MatToTexture(frame[firstFace])
                : frameWithFaceRectangles);
        }
    }
    #endregion [ Unity Lifecycle ]

    #region [ Face Detection ]
    private Mat runFaceDetection()
    {
        // convert webcam feed to be usable by openCv
        Mat frame = OpenCvSharp.Unity.TextureToMat(webCamTexture);

        // find a face
        firstFace = getFirstFace(frame);

        return frame;
    }

    /// <summary> does face detection on the current frame of webCamTexture. sets firstFace to the first face found. </summary>
    private Rect getFirstFace(Mat frame)
    {
        var faces = cascade.DetectMultiScale(frame, 1.1, 2, HaarDetectionType.ScaleImage);
        return (faces.Length >= 1) ? faces[0] : OpenCvSharp.Rect.Empty;
    }

    /// <summary> Adds a bounding box around the first face found </summary>
    private Texture2D addFaceRectangle(Mat frame)
    {
        if (firstFace != OpenCvSharp.Rect.Empty)
        {
            frame.Rectangle(firstFace, new Scalar(250, 0, 0), 2);
            addFaceLabelTo(frame);
        }

        return OpenCvSharp.Unity.MatToTexture(frame);
    }
    #endregion [ Face Detection ]

    #region [ Face Recognition ]
    /// <summary> Data associated with a recognized face </summary>
    private struct Recognized
    {
        public int FaceIndex;
        public double ConfidenceValue;
        public string Confidence;

        public Recognized(int faceIndex, double confidence)
        {
            FaceIndex = faceIndex;
            ConfidenceValue = confidence;
            Confidence = confidence.ToString("0.#");
        }
    }

    /// <summary> Train face recognization model using *.jpg's with https://github.com/shimat/opencvsharp/blob/master/src/OpenCvSharp/Modules/face/FaceRecognizer/FaceRecognizer.cs </summary>
    private LBPHFaceRecognizer trainModelOnFolder(string folderPath, int label)
    {
        // load images from disk as grayscale
        List<Mat> trainingSet = new List<Mat>();
        foreach (var file in Directory.GetFiles(folderPath, "*.jpg")) // create path to all images
        {
            trainingSet.Add(matImageFile(file));
        }

        // create and train model
        var model = LBPHFaceRecognizer.CreateLBPHFaceRecognizer();
        model.Train(trainingSet, System.Linq.Enumerable.Repeat(label, trainingSet.Count));
        return model;
    }

    /// <summary> Uses folder name to set face label </summary>
    /// <returns> Folder of faces model will be trained with </returns>
    private string parseFaceLabel(string startsWith, out string folderPath)
    {
        // get full folder name from file system
        try
        {
            folderPath = Directory
                .GetDirectories(Application.streamingAssetsPath, startsWith + "*.*", SearchOption.AllDirectories)
                .First();
        }
        catch (System.Exception)
        {
            throw new System.Exception($"For training the face model recognition a folder starting with {startsWith} must exist in "
                + Application.streamingAssetsPath);
        }

        // parse face label from folder name
        string label = Path.GetFileName(folderPath).Substring(startsWith.Length);

        // use default if folder name doesn't include a label
        if (string.IsNullOrWhiteSpace(label))
        {
            label = faceLabel;
        }
        return label;
    }

    /// <summary> Uses the recognizer model to determine which label to add to the firstFace found </summary>
    /// <param name="frame"> Mat that the label should be added to </param>
    private void addFaceLabelTo(Mat frame)
    {
        // determine label type
        var recognized = recognizeFirstFace(frame[firstFace]);

        // build label
        string label = (recognized.ConfidenceValue < maxRecognizationConfidence)
            ? $"{faceLabel} {recognized.Confidence}"
            : "Unknown face";
        Debug.Log($"{recognized.FaceIndex} ({recognized.Confidence})");

        // apply label
        addLabelToFrame(ref frame, firstFace, label);
    }

    /// <summary> See if faceInQuestion is found in recognizer model. </summary>
    /// <param name="faceInQuestion"> Bounding box containing just the face to be matched </param>
    /// <returns> faceIndex and confidence that the face is a match </returns>
    private Recognized recognizeFirstFace(Mat faceInQuestion)
    {
        // prepare faceInQuestion to be used; size and grayscale
        Mat smallFace = faceInQuestion.Clone();
        Cv2.Resize(faceInQuestion, smallFace, new Size(256, 256));
        Cv2.CvtColor(smallFace, smallFace, ColorConversionCodes.BGR2GRAY);

        // look for faceInQuestion
        recognizer.Predict(smallFace, out int faceIndex, out double confidence);

        // return result
        return new Recognized(faceIndex, confidence);
    }
    #endregion [ Face Recognition ]

    #region [ Utility ]
    /// <returns> Load image as grayscale from file on local hard drive </returns>
    private static Mat matImageFile(string filePath)
    {
        Mat matResult = null;
        if (File.Exists(filePath))
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            var tex = new Texture2D(2, 2);//, TextureFormat.BGRA32, false);
            tex.LoadImage(fileData); // this will auto-resize the 2,2 texture dimensions.
            matResult = OpenCvSharp.Unity.TextureToMat(tex);

            // change to grayscale
            Cv2.CvtColor(matResult, matResult, ColorConversionCodes.BGR2GRAY);
        }
        return matResult;
    }

    /// <summary> Saves a texture as an image to the local hard drive </summary>
    private void exportImage(Texture2D texture)
    {
        byte[] imageBytes = texture.EncodeToJPG();
        string filePath = Path.Join(sessionFolder, System.DateTime.Now.Ticks.ToString() + ".jpg");
        File.WriteAllBytes(filePath, imageBytes);
    }

    /// <summary> adds a UI label to a frame above the specified location </summary>
    private void addLabelToFrame(ref Mat frame, Rect location, string label)
    {
        frame.PutText(label, offset(location, 0, -5), HersheyFonts.HersheyPlain, 4, Scalar.Blue);
        frame.PutText(label, offset(location, 1, -6), HersheyFonts.HersheyPlain, 4, Scalar.Blue); // add weight to the text
    }

    /// <returns> Offset of the Location field of a face Rect by horizontal & vertical pixels </returns>
    private Point offset(OpenCvSharp.Rect rect, int horizontal, int vertical)
    {
        return rect.Location + new Point(horizontal, vertical);
    }
    #endregion [ Utility ]
}
