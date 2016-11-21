﻿using UnityEngine;
using System.Collections;
using System;
using itk.simple;


public class DICOMLoadReturnObjectSlice {

    public int texWidth { get; set; }
    public int texHeight { get; set; }
    public int texDepth { get; set; }
    public Color32[] colors { get; set; }
    public DICOMHeader header { get; set; }
	public int slice;

	public Image itkImage;

	public DICOMLoadReturnObjectSlice (int texWidth, int texHeight, int texDepth, Color32[] colors, DICOMHeader header, Image image, int slice = -1 )
    {
        this.texWidth = texWidth;
        this.texHeight = texHeight;
        this.texDepth = texDepth;
		this.colors = colors;
		this.header = header;
		this.itkImage = image;
		this.slice = slice;
    }
}
