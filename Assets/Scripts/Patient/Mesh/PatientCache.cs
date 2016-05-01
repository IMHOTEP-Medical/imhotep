﻿using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using System.Collections.Generic;
using System.IO;

public class PatientCache : MonoBehaviour {

    private static PatientCache mInstance;
    private Dictionary<Event, UnityEvent> mEventDictionary;

    private Patient currentPatient;

    public enum Event
    {
        OpenPatient,
        ClosePatient
    }

    // Use this for initialization
    public PatientCache () {
        currentPatient = null;
        //mPatientLoader = new PatientDirectoryLoader();
        //mPatientLoader.setPath("../Patients/");
    }
	
	// Update is called once per frame
	/*void Update () {
	
	}*/


    public void openPatient1(int index)
    {
        //currentPatient = mPatientLoader.loadPatient(index);

		Debug.Log("Path: " + currentPatient.path);

        //DicomCache dicomCache = DicomCache.instance;
		//dicomCache.loadDirectory(currentPatient.path + "/DICOM");

		
    }

    public void closePatient1()
    {
        currentPatient = null;
        MeshLoader mModelLoader = GameObject.Find("GlobalScript").GetComponent<MeshLoader>();
        mModelLoader.RemoveMesh();
    }
}
