using UnityEngine;
using UnityEngine.UI;

public class RealPerspective : MonoBehaviour
{
	private PXCMSenseManager senseManager = null;

    private Slider smoothingSlider;
    private Text trackingText;
	
	private Vector3 previousNormalizedPos;

    private Color darkGray = new Color(0.19f, 0.19f, 0.19f, 1);

    void OnEnable()
	{
        smoothingSlider = GameObject.Find("smoothingSlider").GetComponent<Slider>();
        smoothingSlider.value = 0.95f;

        trackingText = GameObject.Find("faceDetectText").GetComponent<Text>();
        trackingText.text = "Not Tracking";
    }

	void InitializeRealSense()
	{
		pxcmStatus status;

		// Initialize a PXCMSenseManager instance
		senseManager = PXCMSenseManager.CreateInstance ();
		
		if (senseManager == null)
		{
			Debug.LogError ("PXCSenseManager.CreateInstance() failed");

            DisableRealSense();

            return;
		}
		
		status = senseManager.EnableFace();
		if(status != pxcmStatus.PXCM_STATUS_NO_ERROR)
		{
			Debug.Log("PXCSenseManager.EnableFace() failed [" + status + "]");

            DisableRealSense();

            return;
		}

        // Configure face module
        PXCMFaceConfiguration faceCfg;

        // Configure face module for detection (bounding rect and depth)
        faceCfg = senseManager.QueryFace().CreateActiveConfiguration();

        faceCfg.detection.isEnabled = true;
        faceCfg.landmarks.isEnabled = false;
        faceCfg.pose.isEnabled = false;
        faceCfg.detection.maxTrackedFaces = 1;
        faceCfg.detection.smoothingLevel = PXCMFaceConfiguration.SmoothingLevelType.SMOOTHING_HIGH;

        status = faceCfg.ApplyChanges();
        faceCfg.Dispose();

        if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
        {
            Debug.LogError("PXCMFaceConfiguration failed [" + status + "]");

            DisableRealSense();

            return;
        }

        // Initialize sense manager pipeline
        status = senseManager.Init ();
		if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
		{
			Debug.LogError ("PXCSenseManager.Init() failed [" + status + "]");

            DisableRealSense();

            return;
		}
		
		Debug.Log ("RealSense Initialized successfully");
	}

	// Use this for initialization
	void Start ()
	{
		InitializeRealSense ();
	}

    float getFrustumFOV(float t, float b, float n)
    {
        float fov;

        fov = 2.0f * Mathf.Atan( (t - b) / (2.0f * n) );

        return fov;
    }

    void calcOffAxisMatrices(Vector3 pa, Vector3 pb, Vector3 pc, Vector3 pe, float n, float f,
        out Matrix4x4 projectionMatrix, out Matrix4x4 worldToCameraMatrix, out float fieldOfView)
	{
        // Orthonormal basis vectors representing screen
        Vector3 vr, vu, vn;
		
        // right vector
		vr = pb - pa;
		vr.Normalize();
		
        // up vector
		vu = pc - pa;
		vu.Normalize();
		
        // normal vector
		vn = Vector3.Cross (vu, vr);
		vn.Normalize();

        // Screen extent vectors
        Vector3 va, vb, vc;

        // from eye positions (pe) to
        // screen extents (pa, pb, pc)
        va = pa - pe;
		vb = pb - pe;
		vc = pc - pe;

        // Distance from eye position (pe) to
        // the screen-space origin of screen plane
		float d = -Vector3.Dot(vn, va);

        // Frustrum extents on near plane
        float l, r, b, t;

        l = Vector3.Dot(vr, va) * n / d;
		r = Vector3.Dot(vr, vb) * n / d;
		b = Vector3.Dot(vu, va) * n / d;
		t = Vector3.Dot(vu, vc) * n / d;

        // Field of view
        fieldOfView = 180.0f / Mathf.PI * getFrustumFOV(t, b, n);        

		// Perspective projection matrix
		// based on OpenGL's glFrustrum (left-handed)
		Matrix4x4 P = new Matrix4x4();

		P[0,0] =  2.0f * n / (r - l);
		P[0,1] =  0.0f;
		P[0,2] =  (r + l)/(r - l);
		P[0,3] =  0.0f;

		P[1,0] =  0.0f;
		P[1,1] =  2.0f * n / (t - b);
		P[1,2] =  (t + b) / (t - b);
		P[1,3] =  0.0f;

		P[2,0] =  0.0f;
		P[2,1] =  0.0f;
		P[2,2] = -(f + n) / (f - n);
		P[2,3] = -2.0f * f * n / (f - n);

		P[3,0] =  0.0f;
		P[3,1] =  0.0f;
		P[3,2] = -1.0f;
		P[3,3] =  0.0f;

        // Align view with XY plane
        // Mt is transpose of M
		Matrix4x4 Mt = new Matrix4x4();

		Mt [0, 0] = vr.x;
		Mt [0, 1] = vr.y;
		Mt [0, 2] = vr.z;
		Mt [0, 3] = 0.0f;
		
		Mt [1, 0] = vu.x;
		Mt [1, 1] = vu.y;
		Mt [1, 2] = vu.z;
		Mt [1, 3] = 0.0f;
		
		Mt [2, 0] = vn.x;
		Mt [2, 1] = vn.y;
		Mt [2, 2] = vn.z;
		Mt [2, 3] = 0.0f;
		
		Mt [3, 0] = 0.0f;
		Mt [3, 1] = 0.0f;
		Mt [3, 2] = 0.0f;
		Mt [3, 3] = 1.0f;

        // Translate view to origin
		Matrix4x4 T = new Matrix4x4();

		T [0, 0] = 1.0f;
		T [0, 1] = 0.0f;
		T [0, 2] = 0.0f;
		T [0, 3] = -pe.x;
		
		T [1, 0] = 0.0f;
		T [1, 1] = 1.0f;
		T [1, 2] = 0.0f;
		T [1, 3] = -pe.y;
		
		T [2, 0] = 0.0f;
		T [2, 1] = 0.0f;
		T [2, 2] = 1.0f;
		T [2, 3] = -pe.z;
		
		T [3, 0] = 0.0f;
		T [3, 1] = 0.0f;
		T [3, 2] = 0.0f;
		T [3, 3] = 1.0f;

        // Composition and output
		projectionMatrix = P;
		worldToCameraMatrix = Mt * T;
	}

    bool processInput(out Vector3 normalizedPos)
	{
		bool success = false;

		normalizedPos = new Vector3 ();

		// If SenseManager is not available, use mouse data
		if (senseManager == null)
		{
			Vector2 clampMousePos;

			clampMousePos.x = Mathf.Min ( Input.mousePosition.x, Screen.width );
			clampMousePos.x = Mathf.Max ( clampMousePos.x, 0 );

			clampMousePos.y = Mathf.Min ( Input.mousePosition.y, Screen.height );
			clampMousePos.y = Mathf.Max ( clampMousePos.y, 0 );

			normalizedPos.x = (float)clampMousePos.x / Screen.width;
			normalizedPos.y = (float)clampMousePos.y / Screen.height;
			normalizedPos.z = 1.0f;

			success = true;
		}
		else
		{
			pxcmStatus status;

			// Check for frame data
			status = senseManager.AcquireFrame (false, 0);

			if (status == pxcmStatus.PXCM_STATUS_NO_ERROR)
			{
				PXCMFaceModule faceModule;
				PXCMFaceData faceData;
				int numFaces;
				
				faceModule = senseManager.QueryFace ();
				
				faceData = faceModule.CreateOutput ();
				faceData.Update ();

				numFaces = faceData.QueryNumberOfDetectedFaces ();

                

				if ( numFaces > 0 )
				{
					PXCMFaceData.Face[] faces;
                    PXCMFaceData.DetectionData detectionData;

                    trackingText.text = "Face detected";
                    trackingText.color = darkGray;

                    faces = faceData.QueryFaces ();

                    // Picking the first face allows the algorithm
                    // run even if other faces come into view
                    detectionData = faces[0].QueryDetection();

                    if (detectionData != null)
                    {
                        PXCMRectI32 boundingRect;
                        float depth;

                        // used for face x,y
                        detectionData.QueryBoundingRect(out boundingRect);

                        // used for face z
                        detectionData.QueryFaceAverageDepth(out depth);

                        // Normalize 0.0 to 1.0
                        normalizedPos.x = (boundingRect.x + 0.5f * boundingRect.w) / 640.0f;
                        normalizedPos.y = (boundingRect.y + 0.5f * boundingRect.h) / 360.0f;

                        // Empirically found maxDepth = 1000.0f on SR300,
                        // other cameras may be different
                        const float maxDepth = 1000.0f;

                        normalizedPos.z = Mathf.Min(depth / maxDepth, 1.0f);

                        if (depth > 0)
                            success = true;
                    }
                }
				else
				{
					Debug.Log ("No faces detected");
                    trackingText.text = "No faces detected";
                    trackingText.color = Color.red;
                }
				
				senseManager.ReleaseFrame();
			}
		}

		return success;
	}

    void calcOffAxisParameters(Vector3 normalizedPos, out Vector3 pa, out Vector3 pb, out Vector3 pc, out Vector3 pe, out float n, out float f)
	{
		float aspectRatio;
		Vector3 cameraPos;
		float nearClipPlane, farClipPlane;
		float height, width;

		cameraPos = Camera.main.transform.position;
		nearClipPlane = Camera.main.nearClipPlane;
		farClipPlane = Camera.main.farClipPlane;
		aspectRatio	= (float)Screen.width / Screen.height;
        
        height = 9.0f;
        width = height * aspectRatio;

        // The screen plane is defined by tracker/world-space positions: pa, pb, pc
        // They determine the screen's size, aspect ratio, and translation/rotation in space
        //
        // pc
        // +------------+
        // |            |
        // |            |
        // +------------+
        // pa          pb

        pa = new Vector3( -0.5f * width, -0.5f * height, 0.0f );
        pb = new Vector3(  0.5f * width, -0.5f * height, 0.0f );
        pc = new Vector3( -0.5f * width,  0.5f * height, 0.0f );

        // The eye position is calculated from the screen plane and
        // kept within bounds of pa, pb, and pc
        pe.x = (1.0f - normalizedPos.x) * Mathf.Abs(pb.x - pa.x) + pa.x;
        pe.y = (1.0f - normalizedPos.y) * Mathf.Abs(pc.y - pa.y) + pa.y;
        pe.z = -normalizedPos.z         * Mathf.Abs(pa.z - 8.0f);

        n = nearClipPlane;
		f = farClipPlane;
	}
    
	// Update is called once per frame
	void Update ()
	{
        if ( Input.GetKeyDown("escape") )
            Application.Quit();

        Vector3 pa, pb, pc, pe;
		float n, f;

		Vector3 normalizedPos;

		Matrix4x4 projectionMatrix;
		Matrix4x4 worldToCameraMatrix;
        float fieldOfView;

        // Process input either from RealSense face module or mouse
        bool success = processInput (out normalizedPos);
        
        // If new data is available
        if ( success )
		{
			if ( senseManager != null )
			{
                float smoothing = smoothingSlider.value;
				float previousWeight;
				float dist;
				
				dist = Vector3.Distance (normalizedPos, previousNormalizedPos);

                previousWeight = dist < 0.005f ? smoothing : 0.90f;

                normalizedPos = previousWeight * previousNormalizedPos + (1.0f - previousWeight) * normalizedPos;
			}

			previousNormalizedPos = normalizedPos;

            // Calculate off axis parameters
			calcOffAxisParameters (normalizedPos, out pa, out pb, out pc, out pe, out n, out f);

			// Calculate off axis matrices
			calcOffAxisMatrices (pa, pb, pc, pe, n, f, out projectionMatrix, out worldToCameraMatrix, out fieldOfView);

            // Feedback to Unity
            Camera.main.projectionMatrix = projectionMatrix;
			Camera.main.worldToCameraMatrix = worldToCameraMatrix;
            Camera.main.fieldOfView = fieldOfView;
		}
	}

    void DisableRealSense()
    {
        Debug.Log("Disabling RealSense");

        if (senseManager != null)
        {
            senseManager.Close();
            senseManager.Dispose();
            senseManager = null;
        }
    }

	void OnDisable()
	{
        DisableRealSense();
    }
}
