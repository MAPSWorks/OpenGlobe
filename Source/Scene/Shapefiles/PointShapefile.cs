﻿#region License
//
// (C) Copyright 2010 Patrick Cozzi and Deron Ohlarik
//
// Distributed under the Boost Software License, Version 1.0.
// See License.txt or http://www.boost.org/LICENSE_1_0.txt.
//
#endregion

using System;
using System.Drawing;
using System.Collections.Generic;
using MiniGlobe.Core;
using MiniGlobe.Core.Geometry;
using MiniGlobe.Renderer;
using Catfood.Shapefile;

namespace MiniGlobe.Scene
{
    public class PointShapefile : IDisposable
    {
        public PointShapefile(string filename, Context context, Ellipsoid globeShape, Bitmap icon)
        {
            Verify.ThrowIfNull(context);

            if (globeShape == null)
            {
                throw new ArgumentNullException("globeShape");
            }

            using (Shapefile shapefile = new Shapefile(filename))
            {
                if (shapefile.Type == ShapeType.Point)
                {
                    _billboards = new BillboardCollection(context);
                    CreateBillboards(globeShape, shapefile, icon);
                }
                else
                {
                    throw new NotSupportedException("Shapefile type \"" + shapefile.Type.ToString() + "\" is not a point shape file.");
                }
            }
        }

        private void CreateBillboards(Ellipsoid globeShape, Shapefile shapefile, Bitmap iconBitmap)
        {
            Font font = new Font("Arial", 16);
            IList<Bitmap> bitmaps = new List<Bitmap>();
            bitmaps.Add(iconBitmap);
            int labelPixelOffset = iconBitmap.Width / 2;

            foreach (Shape shape in shapefile)
            {
                if (shape.Type == ShapeType.Null)
                {
                    continue;
                }

                if (shape.Type != ShapeType.Point)
                {
                    throw new NotSupportedException("The type of an individual shape does not match the Shapefile type.");
                }

                //
                // TODO: This function is obviously way too hard coded
                //
                bitmaps.Add(Device.CreateBitmapFromText(shape.GetMetadata("nameascii"), font));
                PointD point = (shape as ShapePoint).Point;
                Vector3D position = globeShape.ToVector3D(Trig.ToRadians(new Geodetic3D(point.X, point.Y))); ;

                Billboard icon = new Billboard();
                icon.Position = position;
                _billboards.Add(icon);

                Billboard label = new Billboard();
                label.Position = position;
                label.HorizontalOrigin = HorizontalOrigin.Left;
                label.PixelOffset = new Vector2H(labelPixelOffset, 0);
                _billboards.Add(label);
            }

            TextureAtlas labelAtlas = new TextureAtlas(bitmaps);
            int j = 1;
            for (int i = 0; i < _billboards.Count; i += 2)
            {
                _billboards[i].TextureCoordinates = labelAtlas.TextureCoordinates[0];
                _billboards[i + 1].TextureCoordinates = labelAtlas.TextureCoordinates[j];
                ++j;
            }
            _billboards.Texture = Device.CreateTexture2D(labelAtlas.Bitmap, TextureFormat.RedGreenBlueAlpha8, false);

            for (int i = 1; i < bitmaps.Count; ++i)
            {
                bitmaps[i].Dispose();
            }
            font.Dispose();
        }

        public void Render(SceneState sceneState)
        {
            _billboards.Render(sceneState);
        }

        public bool DepthWrite 
        {
            get { return _billboards.DepthWrite; }
            set { _billboards.DepthWrite = value; }
        }

        public Context Context
        {
            get { return _billboards.Context; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_billboards != null)
            {
                _billboards.Dispose();
            }
        }

        #endregion

        private readonly BillboardCollection _billboards;
    }
}