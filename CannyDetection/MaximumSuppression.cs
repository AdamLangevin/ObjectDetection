﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CannyDetection
{
    class MaximumSuppression
    {

        public static Bitmap Suppression(Bitmap b)
        {
            Bitmap divX;
            Bitmap divY;
            Bitmap grade;
            Bitmap NonMax;

            /*
            BitmapData bData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
                                        ImageLockMode.ReadOnly,
                                        PixelFormat.Format32bppArgb);

            Second order differentiation is needed to get a gradient to determine
            the directions the change is the values are moving in. This helps to determine
            the pixels that are needed to be suppressed.
            */
            grade = (Bitmap)b.Clone();

            divX = (Bitmap)b.Clone();

            ConvMatrix m = new ConvMatrix();
            m.TopLeft = m.MidLeft = m.BottomLeft = 1;
            m.TopRight = m.MidRight = m.BottomRight = -1;
            m.TopMid = m.Pixel = m.BottomMid = 0;

            divX = Filter.Conv(divX, m);

            m.TopLeft = m.TopMid = m.TopRight = 1;
            m.MidLeft = m.MidRight = m.Pixel = 0;
            m.BottomLeft = m.BottomMid = m.BottomRight = -1;

            divY = (Bitmap)b.Clone();

            divY = Filter.Conv(divY, m);

            BitmapData divXData = divX.LockBits(new Rectangle(0, 0, b.Width, b.Height),
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format32bppArgb);

            BitmapData divYData = divY.LockBits(new Rectangle(0, 0, b.Width, b.Height),
                       ImageLockMode.ReadOnly,
                       PixelFormat.Format32bppArgb);
            BitmapData gradeData = grade.LockBits(new Rectangle(0, 0, b.Width, b.Height),
                       ImageLockMode.ReadOnly,
                       PixelFormat.Format32bppArgb);

            int strideX = divXData.Stride;
            int strideY = divYData.Stride;
            int strideG = gradeData.Stride;

            IntPtr Scan0X = divXData.Scan0;
            IntPtr Scan0Y = divYData.Scan0;
            IntPtr Scan0G = gradeData.Scan0;

            unsafe {
                byte* pX = (byte*)(void*)Scan0X;
                byte* pY = (byte*)(void*)Scan0Y;
                byte* pGrade = (byte*)(void*)Scan0G;

                int nOffsetX = strideX - divX.Width * 3;
                int nOffsetY = strideY - divY.Width * 3;
                int nOffsetG = strideG - grade.Width * 3;

                int nWidthX = divX.Width - 2;
                int nWidthY = divY.Width - 2;
                int nWidthG = grade.Width - 2;

                int nHeightX = divX.Height - 2;
                int nHeightY = divY.Height - 2;
                int nHeightG = grade.Height - 2;

                int nPixel;

                for (int y=0; y< nHeightG; y++) {
                    for (int x=0; x< nWidthG; x++) {
                        nPixel = ((int)(Math.Sqrt(pY[2]*pY[2] + pX[2]*pX[2])));
                        pGrade[2+ strideG] = (byte)nPixel;

                        nPixel = ((int)(Math.Sqrt(pY[1]*pY[1] + pX[1]*pX[1])));
                        pGrade[1 + strideG] = (byte)nPixel;

                        nPixel = ((int)(Math.Sqrt(pY[0]*pY[0] + pX[0]*pX[0])));
                        pGrade[0 + strideG] = (byte)nPixel;

                        pY += 3;
                        pX += 3;
                        pGrade += 3;
                    }
                    pY += nOffsetY;
                    pX += nOffsetX;
                    pGrade += nOffsetG;
                }

                //Non Maximum suppression
                //Reduces the fuzzy bits around the solid lines of white areas
                pY = (byte*)(void*)Scan0Y;
                pX = (byte*)(void*)Scan0X;
                pGrade = (byte*)(void*)Scan0G;

                NonMax = (Bitmap)grade.Clone();
                BitmapData NonData = NonMax.LockBits(new Rectangle(0, 0, b.Width, b.Height),
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format32bppArgb);

                byte* pN = (byte*)(void*)NonData.Scan0;
                int nOffsetN = NonData.Stride - NonMax.Width - 2;
                

                int lim = 1;
                float tangent = 0;
                int c1 = 0;
                int c2 = 0; 
                int c3 = 0;

                //Looping through the bitmap, x and y are used as refrences.
                //We lose one pixel around the image, because we are using a 3x3
                //to calculate
                for (int y=lim; y<= nHeightG -lim -1; y++) {
                    for (int x = lim; x <= nWidthG - lim - 1; x++) {
                        c1 = pX[0] + pX[1] + pX[2];
                        c2 = pY[0] + pY[1] + pY[2];
                        if (pX[0] == 0) {
                            tangent = 90F;
                        } else {
                            tangent = (float)(Math.Atan(c1 / c2) * 180 / Math.PI);
                        }

                        //using all three colour channels
                        //Horizontal Edge
                        c1 = pGrade[0] + pGrade[1] + pGrade[2];
                        c2 = pGrade[3] + pGrade[1 + 3] + pGrade[2 + 3];
                        c3 = pGrade[-3] + pGrade[1 - 3] + pGrade[2 - 3];
                        if ((-22.5 < tangent) && (tangent <= 22.5) || (157.5 < tangent) && (tangent <= -157.5)) {
                            if (c1 < c2 || c1 < c3) {
                                pN[0] = 0;
                                pN[1] = 0;
                                pN[2] = 0;
                            }
                        }

                        //Vertical Edge, needs to calculate pixels above and bellow
                        c2 = pGrade[0 + strideG] + pGrade[1 + strideG] + pGrade[2 + strideG];
                        c3 = pGrade[0 - strideG] + pGrade[1 - strideG] + pGrade[2 - strideG];
                        if ((-112.5 < tangent) && (-67.5 <= tangent) || (67.5 < tangent) && (tangent <= 112.5))
                        {
                            if (c1 < c2 || c1 < c3)
                            {
                                pN[0] = 0;
                                pN[1] = 0;
                                pN[2] = 0;
                            }
                        }

                        //+45 Degree Edge
                        c2 = pGrade[0 + 3 + strideG] + pGrade[1 + 3 + strideG] + pGrade[2 + 3 + strideG];
                        c3 = pGrade[0 - 3 + strideG] + pGrade[1 - 3 + strideG] + pGrade[2 - 3 + strideG];
                        if ((-67.5<tangent)&&(-22.5<=tangent) || (112.5<tangent)&&(tangent<=157.5))
                        {
                            if (c1 < c2 || c1 < c3)
                            {
                                pN[0] = 0;
                                pN[1] = 0;
                                pN[2] = 0;
                            }
                        }

                        //-45 Degree Edge
                        c2 = pGrade[0 + 3 + strideG] + pGrade[1 + 3 + strideG] + pGrade[2 + 3 + strideG];
                        c3 = pGrade[0 + 3 + strideG] + pGrade[1 + 3 + strideG] + pGrade[2 + 3 + strideG];
                        if ((-157.5<tangent)&&(-112.5<=tangent) || (67.5<tangent)&&(tangent<=22.5))
                        {
                            if (pGrade[0]<pGrade[0+3+strideG] || pGrade[0]<pGrade[0+3+strideG])
                            {
                                pN[0] = 0;
                                pN[1] = 0;
                                pN[2] = 0;
                            }
                        }

                        c1 = 0;
                        c2 = 0;
                        c3 = 0;
                        pGrade++;
                        pN++;
                    }
                    pGrade += nOffsetG;
                    pN += nOffsetN;
                }


                NonMax.UnlockBits(NonData);
            }//end of unsafe code

            divY.UnlockBits(divYData);
            divX.UnlockBits(divXData);
            grade.UnlockBits(gradeData);
          

            return b;
        }

    }
}