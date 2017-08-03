using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Cv
{
    public class Flandmark
    {
        public class FlandmarkPsiG
        {
            public int[] Disp;
            public int Rows, Cols;
        }

        public class FlandmarkOptions
        {
            public byte M;
            public int[] S;
            public int[] bw = new int[2];
            public int[] bw_margin = new int[2];
            public FlandmarkPsiG[] PsiGS0, PsiGS1, PsiGS2;
            public int[] PsiGRows = new int[3];
            public int[] PsiGCols = new int[3];
        }

        public class FlandmarkLBP
        {
            public int[] WinSize = new int[2];
            public byte HOP;
            public uint[] Wins;
            public uint WinsRows, WinsCols;
        }

        public class FlandmarkData
        {
            public FlandmarkLBP[] LBP;
            public int[] ImSize = new int[2];
            public int[] MapTable;
            public FlandmarkOptions Options = new FlandmarkOptions();
        }

        public class FlandmarkModel
        {
            public double[] W;
            public int WRows, WCols;
            public FlandmarkData Data = new FlandmarkData();
            public byte[] NormalizedImageFrame;
            public double[] bb;
            public float[] sf;
        }

        public class FlandmarkPSI
        {
            public char[] Data;
            public uint PsiRows, PsiCols;
        }

        public class FlandmarkPSISparse
        {
            public uint[] Idxs;
            public uint PsiRows, PsiCols;
        }

        public static int LandmarkNose = 7;
        public static int LandmarkMouthLeft = 3;
        public static int LandmarkMouthRight = 4;
        public static int LandmarkLeftEyeLeft = 5;
        public static int LandmarkLeftEyeRight = 1;
        public static int LandmarkRightEyeLeft = 2;
        public static int LandmarkRightEyeRight = 6;
        public static int LandmarkCenter = 0;

        public static int ModelNose = 0;
        public static int ModelLeftEyeLeft = 1;
        public static int ModelRightEyeRight = 2;
        public static int ModelMouthLeft = 3;
        public static int ModelMouthRight = 4;

        public static List<Point3D> DefaultModel
        {
            get
            {
                return new List<Point3D>()
                {
                    //nose
                    new Point3D(0, 0, 0),
                    //lefteye left
                    new Point3D(-225, -170, 135),
                    //righteye right
                    new Point3D(225, -170, 135),
                    //mouth left
                    new Point3D(-150, 150, 125),
                    //mouth right
                    new Point3D(150, 150, 125),
                };
            }
        }
        
        /// <summary>
        /// Distance between left eye left point to right eye right point in MM. 
        /// </summary>
        public static double EyeDistance { get; set; } = 90;

        /// <summary>
        /// Unit/Millimeter based on eye distance
        /// </summary>
        public static double UnitPerMM => (Math.Abs(DefaultModel[ModelLeftEyeLeft].X) + Math.Abs(DefaultModel[ModelRightEyeRight].X)) / EyeDistance;

        public FlandmarkModel Model;

        public Interpolation Inter { get; set; } = Interpolation.NearestNeighbor;
        
        StringBuilder builder = new StringBuilder();

        public Flandmark(FileNode filename)
        {
            int[] p_int;
            int tsize = -1, tmp_tsize = -1;
            
            Stream fin = filename.Open();
            StreamReader reader = new StreamReader(fin);

            FlandmarkModel tst = new FlandmarkModel();

            fin.Position = 0;

            ReadUntilSpace(reader);
            tst.Data.Options.M = (byte)ReadUntilSpace(reader)[0];

            ReadUntilSpace(reader);
            tst.Data.Options.bw[0] = Convert.ToInt32(ReadUntilSpace(reader));
            tst.Data.Options.bw[1] = Convert.ToInt32(ReadUntilSpace(reader));

            ReadUntilSpace(reader);
            tst.Data.Options.bw_margin[0] = Convert.ToInt32(ReadUntilSpace(reader));
            tst.Data.Options.bw_margin[1] = Convert.ToInt32(ReadUntilSpace(reader));

            ReadUntilSpace(reader);
            tst.WRows = Convert.ToInt32(ReadUntilSpace(reader));
            tst.WCols = Convert.ToInt32(ReadUntilSpace(reader));

            ReadUntilSpace(reader);
            tst.Data.ImSize[0] = Convert.ToInt32(ReadUntilSpace(reader));
            tst.Data.ImSize[1] = Convert.ToInt32(ReadUntilSpace(reader));

            int M = tst.Data.Options.M;

            tst.Data.LBP = new FlandmarkLBP[M];

            for (int i = 0; i < M; i++)
            {
                ReadUntilSpace(reader);

                var lbp = new FlandmarkLBP();

                lbp.WinsRows = (uint)Convert.ToInt32(ReadUntilSpace(reader));
                lbp.WinsCols = (uint)Convert.ToInt32(ReadUntilSpace(reader));

                tst.Data.LBP[i] = lbp;
            }

            for (int i = 0; i < 3; i++)
            {
                ReadUntilSpace(reader);
                tst.Data.Options.PsiGRows[i] = Convert.ToInt32(ReadUntilSpace(reader));
                tst.Data.Options.PsiGCols[i] = Convert.ToInt32(ReadUntilSpace(reader));
            }
            
            fin.Dispose();
            reader.Dispose();

            fin = filename.Open();
            //TODO: fix position
            fin.Position = 111;
            BinaryReader breader = new BinaryReader(fin);

            // load model.W
            tst.W = new double[tst.WRows];

            for (int i = 0; i < tst.WRows; i++)
            {
                tst.W[i] = breader.ReadDouble();
            }

            // load model.data.mapTable
            p_int = new int[M * 4];
            tst.Data.MapTable = new int[M * 4];

            for (int i = 0; i < M * 4; i++)
            {
                p_int[i] = breader.ReadInt32();
            }

            for (int i = 0; i < M * 4; i++)
            {
                tst.Data.MapTable[i] = p_int[i];
            }
            p_int = null;

            // load model.data.lbp
            for (int i = 0; i < M; i++)
            {
                // lbp{idx}.winSize
                p_int = new int[2];

                p_int[0] = breader.ReadInt32();
                p_int[1] = breader.ReadInt32();

                tst.Data.LBP[i].WinSize[0] = p_int[0];
                tst.Data.LBP[i].WinSize[1] = p_int[1];

                p_int = null;

                // lbp{idx}.hop
                tst.Data.LBP[i].HOP = breader.ReadByte();

                // lbp{idx}.wins
                tsize = (int)(tst.Data.LBP[i].WinsRows * tst.Data.LBP[i].WinsCols);

                tst.Data.LBP[i].Wins = new uint[tsize];
                for (int r = 0; r < tsize; r++)
                {
                    tst.Data.LBP[i].Wins[r] = breader.ReadUInt32();
                }
            }

            // load model.options.S
            tst.Data.Options.S = new int[4 * M];

            for (int i = 0; i < 4 * M; i++)
            {
                tst.Data.Options.S[i] = breader.ReadInt32();
            }
            p_int = null;

            // load model.options.PsiG
            FlandmarkPsiG[] PsiGi = null;

            for (int psigs_ind = 0; psigs_ind < 3; psigs_ind++)
            {
                tsize = tst.Data.Options.PsiGRows[psigs_ind] * tst.Data.Options.PsiGCols[psigs_ind];

                switch (psigs_ind)
                {
                    case 0:
                        tst.Data.Options.PsiGS0 = new FlandmarkPsiG[tsize];
                        PsiGi = tst.Data.Options.PsiGS0;
                        break;
                    case 1:
                        tst.Data.Options.PsiGS1 = new FlandmarkPsiG[tsize];
                        PsiGi = tst.Data.Options.PsiGS1;
                        break;
                    case 2:
                        tst.Data.Options.PsiGS2 = new FlandmarkPsiG[tsize];
                        PsiGi = tst.Data.Options.PsiGS2;
                        break;
                }

                int temp = 0;
                for (int i = 0; i < tsize; i++)
                {
                    PsiGi[i] = new FlandmarkPsiG();

                    // disp ROWS
                    temp = breader.ReadInt32();

                    PsiGi[i].Rows = temp;

                    // disp COLS
                    temp = breader.ReadInt32();

                    PsiGi[i].Cols = temp;

                    // disp
                    tmp_tsize = PsiGi[i].Rows * PsiGi[i].Cols;

                    PsiGi[i].Disp = new int[tmp_tsize];

                    for (int r = 0; r < tmp_tsize; r++)
                    {
                        PsiGi[i].Disp[r] = breader.ReadInt32();
                    }
                }
            }

            fin.Dispose();
            breader.Dispose();

            tst.NormalizedImageFrame = new byte[tst.Data.Options.bw[0] * tst.Data.Options.bw[1]];
            tst.bb = new double[4];
            tst.sf = new float[2];

            Model = tst;
        }

        public Point[] Detect(VMat m, int[] boundBox, int[] margin = null)
        {
            double[] landmarks;

            flandmark_detect(ref m, boundBox, ref Model, out landmarks, margin);

            if (landmarks != null)
            {
                Point[] points = new Point[landmarks.Length / 2];
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = new Point(landmarks[i * 2], landmarks[i * 2 + 1]);
                }

                return points;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndex(int r, int c, int nr)
        {
            return c * nr + r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetRow(int i, int r)
        {
            return (i - 1) % r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetCol(int i, int r)
        {
            return (i - 1) / r;
        }

        private string ReadUntilSpace(StreamReader reader)
        {
            char current = char.MinValue;

            while (current != ' ')
            {
                current = (char)reader.Read();
                builder.Append(current);
            }

            string ret = builder.ToString();
            builder.Clear();
            return ret;
        }

        private void flandmark_detect(ref VMat img, int[] bbox, ref FlandmarkModel model, out double[] landmarks, int[] bw_margin = null)
        {
            landmarks = new double[model.Data.Options.M * 2];
            
            if (bw_margin != null)
            {
                model.Data.Options.bw_margin[0] = bw_margin[0];
                model.Data.Options.bw_margin[1] = bw_margin[1];
            }

            if (!flandmark_get_normalized_image_frame(ref img, bbox, ref model.bb, ref model.NormalizedImageFrame, ref model))
            {
                landmarks = null;
                return;
            }

            flandmark_detect_base(model.NormalizedImageFrame, model, landmarks);
            
            model.sf[0] = (float)(model.bb[2] - model.bb[0]) / model.Data.Options.bw[0];
            model.sf[1] = (float)(model.bb[3] - model.bb[1]) / model.Data.Options.bw[1];

            for (int i = 0; i < 2 * model.Data.Options.M; i += 2)
            {
                landmarks[i] = landmarks[i] * model.sf[0] + model.bb[0];
                landmarks[i + 1] = landmarks[i + 1] * model.sf[1] + model.bb[1];
            }
        }

        private bool flandmark_get_normalized_image_frame(ref VMat input, int[] bbox, ref double[] bb, ref byte[] face_img, ref FlandmarkModel model)
        {
            bool flag;
            int[] d = new int[2];
            double[] c = new double[2], nd = new double[2];

            // extend bbox by bw_margin
            d[0] = bbox[2] - bbox[0] + 1; d[1] = bbox[3] - bbox[1] + 1;
            c[0] = (bbox[2] + bbox[0]) / 2.0f; c[1] = (bbox[3] + bbox[1]) / 2.0f;
            nd[0] = d[0] * model.Data.Options.bw_margin[0] / 100.0f + d[0];
            nd[1] = d[1] * model.Data.Options.bw_margin[1] / 100.0f + d[1];

            bb[0] = (c[0] - nd[0] / 2.0f);
            bb[1] = (c[1] - nd[1] / 2.0f);
            bb[2] = (c[0] + nd[0] / 2.0f);
            bb[3] = (c[1] + nd[1] / 2.0f);

            //flag = bb[0] > 0 && bb[1] > 0 && bb[2] < input.Width && bb[3] < input.Height
            //    && bbox[0] > 0 && bbox[1] > 0 && bbox[2] < input.Width && bbox[3] < input.Height;
            //if (!flag)
            //    return false;

            Rect region = new Rect((int)bb[0], (int)bb[1], (int)bb[2] - (int)bb[0] + 1, (int)bb[3] - (int)bb[1] + 1);

            flag = input.Width <= 0 || input.Height <= 0 || region.Width <= 0 || region.Height <= 0;
            if (flag)
                return false;

            Rect clipRegion = new Rect(
                Util.Clamp(region.X, 0, input.Width-1), Util.Clamp(region.Y, 0, input.Height-1), 
                Util.Clamp(region.Width, 0, input.Width), Util.Clamp(region.Height, 0, input.Height));

            using (var resizedImage = VMat.New(input, clipRegion))
            {
                double scalefactor = model.Data.Options.bw[0] / region.Width;
                //resizedImage.Resize(new Size(model.Data.Options.bw[0], model.Data.Options.bw[1]), 0, 0, Inter);
                resizedImage.Resize(new Size(clipRegion.Width * scalefactor, clipRegion.Height * scalefactor), 0, 0, Inter);
                resizedImage.EqualizeHistogram();

                // TODO: parallized
                int step = model.Data.Options.bw[1];
                byte[] face_img_tmp = face_img;
                double regX = region.X, regY = region.Y, inpW = input.Width, inpH = input.Height;
                Parallel.For(0, model.Data.Options.bw[0] * model.Data.Options.bw[1], new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount }, (ind) =>
                {
                    int x = ind / step;
                    int y = ind % step;
                    double preX = x / scalefactor + regX;
                    double preY = y / scalefactor + regY;
                    byte value;
                    // TODO: padding
                    if (preX < 0 || preX >= inpW || preY < 0 || preY >= inpH)
                    {
                        value = 1;
                    }
                    else
                    {
                        int newY = (int)((Util.Clamp(preY, 0, inpH) - clipRegion.Y) * scalefactor);
                        int newX = (int)((Util.Clamp(preX, 0, inpW) - clipRegion.X) * scalefactor);
                        value = resizedImage.At<byte>(newY, newX);
                    }
                    face_img_tmp[x * step + y] = value;
                });
                face_img = face_img_tmp;
                //for (int x = 0; x < model.Data.Options.bw[0]; ++x)
                //{
                //    for (int y = 0; y < model.Data.Options.bw[1]; ++y)
                //    {
                //        face_img[GetIndex(y, x, model.Data.Options.bw[1])] = resizedImage.At<byte>(y, x);
                //    }
                //}
            }

            return true;
        }

        FlandmarkPSISparse[] Cached_Psi_sparse;
        double[] q_temp = null;
        private void flandmark_detect_base(byte[] face_image, FlandmarkModel model, double[] landmarks)
        {
            Profiler.Start("flandmark_detect_base");

            int M = model.Data.Options.M;
            double[] W = model.W;
            int tsize = -1, cols = -1, rows = -1;
            int[] mapTable = model.Data.MapTable;

            if (model.NormalizedImageFrame == null)
            {
                model.NormalizedImageFrame = face_image;
            }

            if(Cached_Psi_sparse == null)
            {
                Cached_Psi_sparse = new FlandmarkPSISparse[M];
            }

            Profiler.Start("flandmark_get_psi_mat_sparse");
            Parallel.For(0, M, new ParallelOptions() { MaxDegreeOfParallelism=M }, (idx) =>
            {
                if (Cached_Psi_sparse[idx] == null)
                    Cached_Psi_sparse[idx] = new FlandmarkPSISparse();

                flandmark_get_psi_mat_sparse(ref Cached_Psi_sparse[idx], ref model, idx);
            });
            Profiler.End("flandmark_get_psi_mat_sparse");

            //for (int idx = 0; idx < M; idx++)
            //{
            //    Psi_sparse[idx] = new FLANDMARK_PSI_SPARSE();
            //    flandmark_get_psi_mat_sparse(ref Psi_sparse[idx], ref model, idx);
            //}

            List<double[]> q = new List<double[]>(M);
            for (int i = 0; i < M; i++)
                q.Add(null);

            List<double[]> g = new List<double[]>(M - 1);
            for (int i = 0; i < M - 1; i++)
                g.Add(null);
            
            int idx_qtemp = 0;

            for (int idx = 0; idx < M; ++idx)
            {
                tsize = mapTable[GetIndex(idx, 1, M)] - mapTable[GetIndex(idx, 0, M)] + 1;

                if(q_temp == null)
                    q_temp = new double[tsize];
                else
                {
                    if (q_temp.Length < tsize)
                        q_temp = new double[tsize];
                }

                Array.Copy(W, mapTable[GetIndex(idx, 0, M)] - 1, q_temp, 0, tsize);

                // sparse dot product <W_q, PSI_q>
                cols = (int)Cached_Psi_sparse[idx].PsiCols;
                rows = (int)Cached_Psi_sparse[idx].PsiRows;
                uint[] psi_temp = Cached_Psi_sparse[idx].Idxs;
                //q[idx] = new double[cols];
                double[] qind = new double[cols];
                for (int i = 0; i < cols; ++i)
                {
                    double dotprod = 0.0f;
                    for (int j = 0; j < rows; ++j)
                    {
                        idx_qtemp = (int)psi_temp[(rows * i) + j];
                        dotprod += q_temp[idx_qtemp];
                    }
                    qind[i] = dotprod;
                }
                q[idx] = qind;

                if (idx > 0)
                {
                    tsize = mapTable[GetIndex(idx, 3, M)] - mapTable[GetIndex(idx, 2, M)] + 1;
                    g[idx - 1] = new double[tsize];
                    Array.Copy(W, mapTable[GetIndex(idx, 2, M)] - 1, g[idx - 1], 0, tsize);
                }
            }

            Profiler.Start("flandmark_argmax");
            flandmark_argmax(landmarks, ref model.Data.Options, mapTable, Cached_Psi_sparse, q, g);
            Profiler.End("flandmark_argmax");

            g.Clear();
            q.Clear();

            Profiler.End("flandmark_detect_base");
        }

        private void flandmark_get_psi_mat_sparse(ref FlandmarkPSISparse Psi, ref FlandmarkModel model, int lbpidx)
        {
            //uint[] Features;
            byte[] Images = model.NormalizedImageFrame;
            uint im_H = (uint)model.Data.ImSize[0];
            uint im_W = (uint)model.Data.ImSize[1];
            uint[] Wins = model.Data.LBP[lbpidx].Wins;
            UInt16 win_H = (UInt16)model.Data.LBP[lbpidx].WinSize[0];
            UInt16 win_W = (UInt16)model.Data.LBP[lbpidx].WinSize[1];
            UInt16 nPyramids = model.Data.LBP[lbpidx].HOP;
            uint nDim = LibLBP.PyrGetDim(win_H, win_W, nPyramids) / 256;
            uint nData = model.Data.LBP[lbpidx].WinsCols;

            uint cnt0, mirror, x, x1, y, y1, idx;
            uint[] win;

            if (Psi.Idxs == null) // Psi.idxs.Length != nDim * nData)
            {
                Psi.Idxs = new uint[nDim * nData];
            }

            win = new uint[win_H * win_W];

            for (uint i = 0; i < nData; ++i)
            {
                idx = Wins[GetIndex(0, (int)i, 4)] - 1;
                x1 = Wins[GetIndex(1, (int)i, 4)] - 1;
                y1 = Wins[GetIndex(2, (int)i, 4)] - 1;
                mirror = Wins[GetIndex(3, (int)i, 4)];

                int img_ptr = (int)(idx * im_H * im_W);

                cnt0 = 0;

                if (mirror == 0)
                {
                    for (x = x1; x < x1 + win_W; x++)
                        for (y = y1; y < y1 + win_H; y++)
                            win[cnt0++] = Images[img_ptr + GetIndex((int)y, (int)x, (int)im_H)];
                }
                else
                {
                    for (x = x1 + win_W - 1; x >= x1; x--)
                        for (y = y1; y < y1 + win_H; y++)
                            win[cnt0++] = Images[img_ptr + GetIndex((int)y, (int)x, (int)im_H)];
                }

                LibLBP.PyrFeaturesSparse(ref Psi.Idxs, nDim, win, win_H, win_W, nDim * i);
            }

            Psi.PsiCols = nData;
            Psi.PsiRows = nDim;
            //Psi.idxs = Features;
        }

        private void flandmark_argmax(double[] smax, ref FlandmarkOptions options, int[] mapTable, FlandmarkPSISparse[] Psi_sparse, List<double[]> q, List<double[]> g)
        {
            byte M = options.M;

            int[] indices = new int[M];
            int tsize = mapTable[GetIndex(1, 3, M)] - mapTable[GetIndex(1, 2, M)] + 1;

            // left branch - store maximum and index of s5 for all positions of s1
            int q1_length = (int)Psi_sparse[1].PsiCols;

            double[] s1 = new double[2 * q1_length];
            double[] s1_maxs = new double[q1_length];

            for (int i = 0; i < q1_length; ++i)
            {
                // dot product <g_5, PsiGS1>
                flandmark_maximize_gdotprod(
                        //s2_maxs, s2_idxs,
                        ref s1[GetIndex(0, i, 2)], ref s1[GetIndex(1, i, 2)],
                        q[5], g[4], options.PsiGS1[GetIndex(i, 0, options.PsiGRows[1])].Disp,
                        options.PsiGS1[GetIndex(i, 0, options.PsiGRows[1])].Cols, tsize
                        );
                s1[GetIndex(0, i, 2)] += q[1][i];
            }

            for (int i = 0; i < q1_length; ++i)
            {
                s1_maxs[i] = s1[GetIndex(0, i, 2)];
            }

            // right branch (s2->s6) - store maximum and index of s6 for all positions of s2
            int q2_length = (int)Psi_sparse[2].PsiCols;
            double[] s2 = new double[2 * q2_length];
            double[] s2_maxs = new double[q2_length];

            for (int i = 0; i < q2_length; ++i)
            {
                // dot product <g_6, PsiGS2>
                flandmark_maximize_gdotprod(
                        //s2_maxs, s2_idxs,
                        ref s2[GetIndex(0, i, 2)], ref s2[GetIndex(1, i, 2)],
                        q[6], g[5], options.PsiGS2[GetIndex(i, 0, options.PsiGRows[2])].Disp,
                        options.PsiGS2[GetIndex(i, 0, options.PsiGRows[2])].Cols, tsize);
                s2[GetIndex(0, i, 2)] += q[2][i];
            }

            for (int i = 0; i < q2_length; ++i)
            {
                s2_maxs[i] = s2[GetIndex(0, i, 2)];
            }

            int q0_length = (int)Psi_sparse[0].PsiCols;
            double maxs0 = -float.MaxValue;
            int maxs0_idx = -1;
            double maxq10 = -float.MaxValue, maxq20 = -float.MaxValue, maxq30 = -float.MaxValue, maxq40 = -float.MaxValue, maxq70 = -float.MaxValue;
            double[] s0 = new double[M * q0_length];

            for (int i = 0; i < q0_length; ++i)
            {
                // q10
                maxq10 = -float.MaxValue;
                flandmark_maximize_gdotprod(
                        ref maxq10, ref s0[GetIndex(1, i, M)],
                        s1_maxs, g[0], options.PsiGS0[GetIndex(i, 0, options.PsiGRows[0])].Disp,
                        options.PsiGS0[GetIndex(i, 0, options.PsiGRows[0])].Cols, tsize);
                s0[GetIndex(5, i, M)] = s1[GetIndex(1, (int)s0[GetIndex(1, i, M)], 2)];
                // q20
                maxq20 = -float.MaxValue;
                flandmark_maximize_gdotprod(
                        ref maxq20, ref s0[GetIndex(2, i, M)],
                        s2_maxs, g[1], options.PsiGS0[GetIndex(i, 1, options.PsiGRows[0])].Disp,
                        options.PsiGS0[GetIndex(i, 1, options.PsiGRows[0])].Cols, tsize);
                s0[GetIndex(6, i, M)] = s2[GetIndex(1, (int)s0[GetIndex(2, i, M)], 2)];
                // q30
                maxq30 = -float.MaxValue;
                flandmark_maximize_gdotprod(
                        ref maxq30, ref s0[GetIndex(3, i, M)],
                        q[3], g[2], options.PsiGS0[GetIndex(i, 2, options.PsiGRows[0])].Disp,
                        options.PsiGS0[GetIndex(i, 2, options.PsiGRows[0])].Cols, tsize);
                // q40
                maxq40 = -float.MaxValue;
                flandmark_maximize_gdotprod(
                        ref maxq40, ref s0[GetIndex(4, i, M)],
                        q[4], g[3], options.PsiGS0[GetIndex(i, 3, options.PsiGRows[0])].Disp,
                        options.PsiGS0[GetIndex(i, 3, options.PsiGRows[0])].Cols, tsize);
                // q70
                maxq70 = -float.MaxValue;
                flandmark_maximize_gdotprod(
                        ref maxq70, ref s0[GetIndex(7, i, M)],
                        q[7], g[6], options.PsiGS0[GetIndex(i, 4, options.PsiGRows[0])].Disp,
                        options.PsiGS0[GetIndex(i, 4, options.PsiGRows[0])].Cols, tsize);
                // sum q10+q20+q30+q40+q70
                if (maxs0 < maxq10 + maxq20 + maxq30 + maxq40 + maxq70 + q[0][i])
                {
                    maxs0_idx = i;
                    s0[GetIndex(0, i, M)] = i;
                    maxs0 = maxq10 + maxq20 + maxq30 + maxq40 + maxq70 + q[0][i];
                }
            }

            // get indices
            for (int i = 0; i < M; ++i)
            {
                indices[i] = (int)s0[GetIndex(0, maxs0_idx, M) + i] + 1;
            }

            for (int i = 0; i < M; ++i)
            {
                int rows = options.S[GetIndex(3, i, 4)] - options.S[GetIndex(1, i, 4)] + 1;
                smax[GetIndex(0, i, 2)] = (double)(GetCol(indices[i], rows) + options.S[GetIndex(0, i, 4)]);
                smax[GetIndex(1, i, 2)] = (double)(GetRow(indices[i], rows) + options.S[GetIndex(1, i, 4)]);
            }
        }

        private void flandmark_maximize_gdotprod(ref double maximum, ref double idx, double[] first, double[] second, int[] third, int cols, int tsize)
        {
            double dotprod = 0;

            maximum = -float.MaxValue;
            idx = -1;
            for (int dp_i = 0; dp_i < cols; ++dp_i)
            {
                dotprod = 0;
                for (int dp_j = 0; dp_j < tsize; ++dp_j)
                {
                    dotprod += second[dp_j] * third[dp_i * tsize + dp_j];
                }

                if (maximum < first[dp_i] + dotprod)
                {
                    idx = dp_i;
                    maximum = first[dp_i] + dotprod;
                }
            }
        }
    }
}
