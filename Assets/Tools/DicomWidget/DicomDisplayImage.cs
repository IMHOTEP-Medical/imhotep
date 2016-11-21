﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System;
using UI;
using itk.simple;

public class DicomDisplayImage : MonoBehaviour, IScrollHandler, IPointerDownHandler, IPointerUpHandler, IPointerHoverHandler {

	private Material mMaterial;
	//private float mMinValue;
	//private float mMaxValue;
	private int mLayer;

	// Positioning:
	ViewSettings currentViewSettings = new ViewSettings();
	/*private Slider mMinSlider;
	private Slider mMaxSlider;
	private Slider mLayerSlider;*/

	// When dragLevelWindow is true, moving the mouse will modify the windowing:
	private bool dragLevelWindow = false;
	// When dragPan is true, moving the mouse will modify the position:
	private bool dragPan = false;
	// When dragZoom is true, moving the mouse will modify the position:
	private bool dragZoom = false;

	private DICOMSlice currentDICOM;

	private struct ViewSettings
	{
		public float level;
		public float window;
		public float panX;
		public float panY;
		public float zoom;
		public bool flipHorizontal;
		public bool flipVertical;
	}

	private Dictionary<string, ViewSettings> savedViewSettings = new Dictionary<string, ViewSettings>();

	public UI.Widget widget;

	// Use this for initialization
	void Awake () {
		//mMinValue = 0.0f;
		//mMaxValue = 1.0f;
		currentViewSettings = new ViewSettings {
			level = 0.5f,
			window = 1f,
			panX = 0f,
			panY = 0f,
			zoom = 1f,
			flipHorizontal = false,
			flipVertical = true
		};

		mLayer = 0;
		dragLevelWindow = false;

		mMaterial = new Material (Shader.Find ("Unlit/DICOM2D"));
		GetComponent<RawImage> ().material = mMaterial;

		//clear ();
	}

	public void OnScroll(PointerEventData eventData)
	{
		if (currentDICOM != null) {
			//int numLayers = (int)currentDICOM.getHeader ().NumberOfImages;
			int scrollAmount = Mathf.RoundToInt( eventData.scrollDelta.y*0.2f );
			if( Mathf.Abs(scrollAmount) > 0 )
			{
				LayerChanged (mLayer + scrollAmount);
			}
		}
		//mLayerSlider.value = mLayer;
	}
	public void OnPointerDown( PointerEventData eventData )
	{
		if( eventData.button == PointerEventData.InputButton.Left )
			dragLevelWindow = true;
		else if( eventData.button == PointerEventData.InputButton.Right )
			dragPan = true;
		else if( eventData.button == PointerEventData.InputButton.Middle )
			dragZoom = true;
	}
	public void OnPointerUp( PointerEventData eventData )
	{
		if( eventData.button == PointerEventData.InputButton.Left )
			dragLevelWindow = false;
		else if( eventData.button == PointerEventData.InputButton.Right )
			dragPan = false;
		else if( eventData.button == PointerEventData.InputButton.Middle )
			dragZoom = false;
	}

	public void OnPointerHover( PointerEventData eventData )
	{
		if(currentDICOM != null)
		{
			// Cast event data to CustomEventData:
			CustomEventData cEventData = eventData as CustomEventData;
			if (cEventData != null) {	// Just in case

				// Calculate which pixel in the dicom was hit:
				Vector3 pixel = uvToPixel (cEventData.textureCoord);
				//pixel = Vector3.zero;
				Debug.Log ("Pixel: " + pixel);
				// Calculate which 3D-Position (in the patient coordinate system) this pixel represents:
				Vector3 pos3D = pixelTo3DPos (pixel);
				Debug.Log ("pos3D: " + pos3D);

				VectorInt64 index = new VectorInt64();
				index.Add( (int)pixel.x );
				index.Add( (int)pixel.y );
				index.Add( (int)pixel.z );
				VectorDouble pos = currentDICOM.image.TransformIndexToPhysicalPoint (index);
				pos3D.x = -(float)pos [0];
				pos3D.y = -(float)pos [1];
				pos3D.z = -(float)pos [2];
				Debug.Log ("pos3D 2: " + pos3D);

				// Display the current position:
				Text t = transform.FindChild ("PositionText").GetComponent<Text> ();
				t.text = "(" + (int)Mathf.Round(pixel.x) + ", " + (int)Mathf.Round(pixel.y) + ", " + pixel.z + ")";

				GameObject pointer = GameObject.Find ("3DPointer");
				if (pointer != null)
					pointer.transform.localPosition = pos3D;
			}
		}
	}

	public Vector3 uvToPixel( Vector2 uv )
	{
		// Transfer the uv-coordinate in the space of the full DICOM window to
		// uv-coordinates for the current layer:
		Vector2 dicomUV = imageUVtoLayerUV (uv);
		// Calculate which pixel this uv represents:
		Vector3 pixel = new Vector3 (dicomUV.x * currentDICOM.getTexture2D ().width,
			dicomUV.y * currentDICOM.getTexture2D ().height,
			mLayer);

		return pixel;
	}

	public Vector3 pixelTo3DPos( Vector3 pixel )
	{
		DICOMHeader header = currentDICOM.getHeader ();

		Vector3 positionDICOM = Vector3.Scale (pixel, header.getSpacing ());
		Vector3 positionUnity = - header.getDirectionCosineX () * positionDICOM.x
		                        - header.getDirectionCosineY () * positionDICOM.y
								- header.getDirectionCosineZ () * positionDICOM.z;
		Debug.Log ("positionUnity: " + positionUnity);
		Vector3 origin = header.getOrigin ();
		Debug.Log ("origin: " + origin);
		Debug.Log ("Spacing: " + header.getSpacing ());
		positionUnity += new Vector3 (-origin.x, -origin.y, -origin.z);
		return positionUnity;
	}

	public Vector2 imageUVtoLayerUV( Vector2 imageUV )
	{
		Rect uvRect = GetComponent<RawImage> ().uvRect;
		Vector2 uv = imageUV;
		//uv.Scale (uvRect.size);
		uv = uv + new Vector2( uvRect.min.x/uvRect.width, uvRect.min.y/uvRect.height );
		uv.Scale (uvRect.size);
		return uv;
	}

	public void Update()
	{
		InputDevice inputDevice = InputDeviceManager.instance.currentInputDevice;
		if (inputDevice.getDeviceType () == InputDeviceManager.InputDeviceType.Mouse) {
			if (dragLevelWindow) {
				
				float intensityChange = -inputDevice.getTexCoordDelta ().y * 0.25f;
				float contrastChange = inputDevice.getTexCoordDelta ().x * 0.5f;

				SetLevel (currentViewSettings.level + intensityChange);
				SetWindow (currentViewSettings.window + contrastChange);
			}
			if (dragPan) {

				float dX = -inputDevice.getTexCoordDelta ().x;
				float dY = -inputDevice.getTexCoordDelta ().y;
				if (currentViewSettings.flipHorizontal)
					dX = -dX;
				if (currentViewSettings.flipVertical)
					dY = -dY;

				currentViewSettings.panX += dX * currentViewSettings.zoom;
				currentViewSettings.panY += dY * currentViewSettings.zoom;

				ApplyScaleAndPosition ();
			}
			if (dragZoom) {

				float dY = -inputDevice.getTexCoordDelta ().y * 0.5f;

				currentViewSettings.zoom = Mathf.Clamp (currentViewSettings.zoom + dY, 0.1f, 5f);

				ApplyScaleAndPosition ();
			}
		}

		// Let controller movement change position and zoom (if trigger is pressed):
		if (inputDevice.getDeviceType () == InputDeviceManager.InputDeviceType.ViveController) {
			Controller c = inputDevice as Controller;
			if (c != null) {
				if (c.triggerPressed ()) {
					// Get movement delta:
					Vector3 movement = c.positionDelta;

					// Transform the movement into the local space of the screen's Transform, to see if we're
					// moving away from, towards, left, right, up or down relative to the screen:
					UnityEngine.Transform tf = Platform.instance.getCenterTransformForScreen (widget.layoutPosition.screen);
					movement = tf.InverseTransformDirection (movement);

					float dZ = -movement.z*2f*currentViewSettings.zoom;
					currentViewSettings.zoom = Mathf.Clamp (currentViewSettings.zoom + dZ, 0.1f, 5f);

					float dX = movement.x;
					float dY = movement.y;
					currentViewSettings.panX += dX*currentViewSettings.zoom;
					currentViewSettings.panY += dY*currentViewSettings.zoom;

					ApplyScaleAndPosition ();

				}
			}
		}

		LeftController lc = InputDeviceManager.instance.leftController;
		if (lc != null) {
			Vector2 scrollDelta = lc.touchpadDelta * 200;

			float intensityChange = -scrollDelta.y / 2000f;
			float contrastChange = scrollDelta.x / 2000f;

			SetLevel (currentViewSettings.level + intensityChange);
			SetWindow (currentViewSettings.window + contrastChange);
		}
	}

	public void SetLevel( float newLevel )
	{
		currentViewSettings.level = Mathf.Clamp (newLevel, -0.5f, 1.5f);
		UpdateLevelWindow ();
		SaveViewSettings ();
	}

	public void SetWindow( float newWindow )
	{
		currentViewSettings.window = Mathf.Clamp (newWindow, 0f, 1f);
		UpdateLevelWindow ();
		SaveViewSettings ();
	}

	private void UpdateLevelWindow()
	{
		if (mMaterial == null)
			return;

		mMaterial.SetFloat ("level", currentViewSettings.level);
		mMaterial.SetFloat ("window", currentViewSettings.window);
	}

	private void SaveViewSettings()
	{
		if (currentDICOM == null)
			return;

		string seriesUID = currentDICOM.getHeader ().SeriesUID;
		if (savedViewSettings.ContainsKey (seriesUID)) {
			savedViewSettings [seriesUID] = currentViewSettings;
		} else {
			savedViewSettings.Add (seriesUID, currentViewSettings);
		}
	}

	private void LoadViewSettings()
	{
		if (currentDICOM == null)
			return;

		string seriesUID = currentDICOM.getHeader ().SeriesUID;
		if (savedViewSettings.ContainsKey (seriesUID)) {
			currentViewSettings = savedViewSettings [seriesUID];
		} else {
			currentViewSettings = new ViewSettings {
				level = 0.5f,
				window = 1f,
				panX = 0f,
				panY = 0f,
				zoom = 1f,
				flipHorizontal = false,
				flipVertical = true
			};
		}

		UpdateLevelWindow ();
		ApplyScaleAndPosition ();
	}

	/*public void MinChanged( float newVal )
	{
		if (mMaterial == null)
			return;
		mMinValue = newVal;
		mMaterial.SetFloat ("minValue", mMinValue);
	}

	public void MaxChanged( float newVal )
	{
		if (mMaterial == null)
			return;
		mMaxValue = newVal;
		mMaterial.SetFloat ("maxValue", mMaxValue);
	}*/

	public void LayerChanged( float newVal )
	{
		if (currentDICOM != null) {
			int numLayers = (int)currentDICOM.getHeader ().NumberOfImages;
			//mMaterial.SetFloat ("layer", mLayer*mFilledPartOfTexture);
			mLayer = (int)Mathf.Clamp (newVal, 0, numLayers - 1);
			//Debug.Log ("Layer: " + mLayer + "/" + (int)currentDICOM.getHeader ().NumberOfImages);

			PatientDICOMLoader mPatientDICOMLoader = GameObject.Find("GlobalScript").GetComponent<PatientDICOMLoader>();
			mPatientDICOMLoader.loadDicomSlice ( mLayer );
		}
	}

	public float frac( float val )
	{
		return val - Mathf.Floor (val);
	}

	public void SetDicom( DICOMSlice dicom )
	{
		if (mMaterial == null)
			return;
		Texture2D tex = dicom.getTexture2D ();

		mLayer = dicom.slice;
		//GetComponent<RectTransform> ().sizeDelta = new Vector2 (newWidth, newHeight);
		/*Debug.LogWarning("Min, max: " + dicom.getMinimum () + " " + dicom.getMaximum () );
		mMaterial.SetFloat ("globalMaximum", (float)dicom.getMaximum ());
		mMaterial.SetFloat ("globalMinimum", (float)dicom.getMinimum ());
		mMaterial.SetFloat ("range", (float)(dicom.getMaximum () - dicom.getMinimum ()));*/

		mMaterial.SetFloat ("globalMinimum", (float)dicom.getHeader().MinPixelValue);
		mMaterial.SetFloat ("globalMaximum", (float)dicom.getHeader().MaxPixelValue);

		GetComponent<RawImage> ().texture = tex;

		currentDICOM = dicom;

		LoadViewSettings ();
	}

	public void ApplyScaleAndPosition()
	{
		Texture2D tex = GetComponent<RawImage> ().texture as Texture2D;

		float scaleW = 1f;
		float scaleH = 1f;
		// Get the pixel-spacing from the DICOM header:
		Vector3 spacing = new Vector3 ();
		spacing.x = (float)currentDICOM.getHeader ().Spacing [0];
		spacing.y = (float)currentDICOM.getHeader ().Spacing [1];
		//spacing.z = (float)currentDICOM.getHeader ().Spacing [2];
		// Number of pixels multiplied with the spacing of a pixel gives the texture width/height:
		float effectiveWidth = tex.width * spacing.x;
		float effectiveHeight = tex.height * spacing.y;
		// Scale to the correct aspect ratio:
		if (effectiveWidth > effectiveHeight) {
			scaleH = (float)effectiveWidth / (float)effectiveHeight;
		} else {
			scaleW = (float)effectiveHeight / (float)effectiveWidth;
		}

		float oX = currentViewSettings.panX*scaleW;
		float oY = currentViewSettings.panY*scaleH;

		if (currentViewSettings.flipHorizontal)
			scaleW = scaleW * -1;
		if (currentViewSettings.flipVertical)
			scaleH = scaleH * -1;

		Rect uvRect = GetComponent<RawImage> ().uvRect;
		uvRect.size = new Vector2 (scaleW*currentViewSettings.zoom, scaleH*currentViewSettings.zoom);
		uvRect.center = new Vector2 (0.5f + oX, 0.5f + oY);
		GetComponent<RawImage> ().uvRect = uvRect;

		SaveViewSettings ();
	}

	public void FlipHorizontal()
	{
		Rect uvRect = GetComponent<RawImage> ().uvRect;
		uvRect.width = -uvRect.width;
		GetComponent<RawImage> ().uvRect = uvRect;
	}
	public void FlipVertical()
	{
		Rect uvRect = GetComponent<RawImage> ().uvRect;
		uvRect.height = -uvRect.height;
		GetComponent<RawImage> ().uvRect = uvRect;
	}
}
