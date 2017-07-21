using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vision.Cv
{
    public class Flandmark
    {
        enum EError_T
        {
            NO_ERR = 0,
            ERROR_M = 1,
            ERROR_BW = 2,
            ERROR_BW_MARGIN = 3,
            ERROR_W = 4,
            ERROR_DATA_IMAGES = 5,
            ERROR_DATA_MAPTABLE = 6,
            ERROR_DATA_LBP = 7,
            ERROR_DATA_OPTIONS_S = 8,
            ERROR_DATA_OPTIONS_PSIG = 9,
            UNKNOWN_ERROR = 100
        };

        class FLANDMARK_PSIG
        {
            public int[] disp;
            public int ROWS, COLS;
        }

        class FLANDMARK_Options
        {
            public byte M;
            public int[] S;
            public int[] bw = new int[2];
            public int[] bw_margin = new int[2];
            public FLANDMARK_PSIG[] PsiGS0, PsiGS1, PsiGS2;
            public int[] PSIG_ROWS = new int[3];
            public int[] PSIG_COLS = new int[3];
        }

        class FLANDMARK_LBP
        {
            public int[] winSize = new int[2];
            public byte hop;
            public uint[] wins;
            public uint WINS_ROWS, WINS_COLS;
        }

        class FLANDMARK_Data
        {
            public FLANDMARK_LBP[] lbp;
            public int[] imSize = new int[2];
            public int[] mapTable;
            public FLANDMARK_Options options = new FLANDMARK_Options();
        }

        class FLANDMARK_Model
        {
            public double[] W;
            public int W_ROWS, W_COLS;
            public FLANDMARK_Data data = new FLANDMARK_Data();
            public byte[] normalizedImageFrame;
            public double[] bb;
            public float[] sf;
        }

        class FLANDMARK_PSI
        {
            public char[] data;
            public uint PSI_ROWS, PSI_COLS;
        }

        class FLANDMARK_PSI_SPARSE
        {
            public uint[] idxs;
            public uint PSI_ROWS, PSI_COLS;
        }

        FLANDMARK_Model model;

        public Interpolation Inter { get; set; } = Interpolation.NearestNeighbor;

        public static int Nose = 7;
        public static int MouthLeft = 3;
        public static int MouthRight = 4;
        public static int LeftEyeLeft = 5;
        public static int LeftEyeRight = 1;
        public static int RightEyeLeft = 2;
        public static int RightEyeRight = 6;
        public static int LandmarkCenter = 0;
        public static List<Point3D> DefaultModel
        {
            get
            {
                List<Point3D> model_points = new List<Point3D>()
                {
                    //nose
                    new Point3D(0, 0, 0),
                    //lefteye left
                    new Point3D(-225, 170, -135),
                    //righteye right
                    new Point3D(225, 170, -135),
                    //mouth left
                    new Point3D(-150, -150, -125),
                    //mouth right
                    new Point3D(150, -150, -125),
                };
                return model_points;
            }
        }
        
        public Flandmark(FileNode filename)
        {
            int[] p_int;
            int tsize = -1, tmp_tsize = -1;
            
            Stream fin = filename.Open();
            StreamReader reader = new StreamReader(fin);

            // allocate memory for FLANDMARK_Model
            FLANDMARK_Model tst = new FLANDMARK_Model();

            fin.Position = 0;

            ReadUntilSpace(reader);
            tst.data.options.M = (byte)ReadUntilSpace(reader)[0];

            ReadUntilSpace(reader);
            tst.data.options.bw[0] = Convert.ToInt32(ReadUntilSpace(reader));
            tst.data.options.bw[1] = Convert.ToInt32(ReadUntilSpace(reader));

            ReadUntilSpace(reader);
            tst.data.options.bw_margin[0] = Convert.ToInt32(ReadUntilSpace(reader));
            tst.data.options.bw_margin[1] = Convert.ToInt32(ReadUntilSpace(reader));

            ReadUntilSpace(reader);
            tst.W_ROWS = Convert.ToInt32(ReadUntilSpace(reader));
            tst.W_COLS = Convert.ToInt32(ReadUntilSpace(reader));

            ReadUntilSpace(reader);
            tst.data.imSize[0] = Convert.ToInt32(ReadUntilSpace(reader));
            tst.data.imSize[1] = Convert.ToInt32(ReadUntilSpace(reader));

            int M = tst.data.options.M;

            tst.data.lbp = new FLANDMARK_LBP[M];

            for (int i = 0; i < M; i++)
            {
                ReadUntilSpace(reader);

                var lbp = new FLANDMARK_LBP();

                lbp.WINS_ROWS = (uint)Convert.ToInt32(ReadUntilSpace(reader));
                lbp.WINS_COLS = (uint)Convert.ToInt32(ReadUntilSpace(reader));

                tst.data.lbp[i] = lbp;
            }

            for (int i = 0; i < 3; i++)
            {
                ReadUntilSpace(reader);
                tst.data.options.PSIG_ROWS[i] = Convert.ToInt32(ReadUntilSpace(reader));
                tst.data.options.PSIG_COLS[i] = Convert.ToInt32(ReadUntilSpace(reader));
            }
            
            fin.Dispose();
            reader.Dispose();

            fin = filename.Open();
            fin.Position = 111;
            BinaryReader breader = new BinaryReader(fin);

            // load model.W -----------------------------------------------------------
            tst.W = new double[tst.W_ROWS];

            for (int i = 0; i < tst.W_ROWS; i++)
            {
                tst.W[i] = breader.ReadDouble();
            }

            // load model.data.mapTable -----------------------------------------------
            p_int = new int[M * 4];
            tst.data.mapTable = new int[M * 4];

            for (int i = 0; i < M * 4; i++)
            {
                p_int[i] = breader.ReadInt32();
            }

            for (int i = 0; i < M * 4; i++)
            {
                tst.data.mapTable[i] = p_int[i];
            }
            p_int = null;

            // load model.data.lbp ---------------------------------------------------
            for (int i = 0; i < M; i++)
            {
                // lbp{idx}.winSize
                p_int = new int[2];

                p_int[0] = breader.ReadInt32();
                p_int[1] = breader.ReadInt32();

                tst.data.lbp[i].winSize[0] = p_int[0];
                tst.data.lbp[i].winSize[1] = p_int[1];

                p_int = null;

                // lbp{idx}.hop
                tst.data.lbp[i].hop = breader.ReadByte();

                // lbp{idx}.wins
                tsize = (int)(tst.data.lbp[i].WINS_ROWS * tst.data.lbp[i].WINS_COLS);

                tst.data.lbp[i].wins = new uint[tsize];
                for (int r = 0; r < tsize; r++)
                {
                    tst.data.lbp[i].wins[r] = breader.ReadUInt32();
                }
            }

            // load model.options.S --------------------------------------------------
            tst.data.options.S = new int[4 * M];

            for (int i = 0; i < 4 * M; i++)
            {
                tst.data.options.S[i] = breader.ReadInt32();
            }
            p_int = null;

            // load model.options.PsiG -----------------------------------------------
            FLANDMARK_PSIG[] PsiGi = null;

            for (int psigs_ind = 0; psigs_ind < 3; psigs_ind++)
            {
                tsize = tst.data.options.PSIG_ROWS[psigs_ind] * tst.data.options.PSIG_COLS[psigs_ind];

                switch (psigs_ind)
                {
                    case 0:
                        tst.data.options.PsiGS0 = new FLANDMARK_PSIG[tsize];
                        PsiGi = tst.data.options.PsiGS0;
                        break;
                    case 1:
                        tst.data.options.PsiGS1 = new FLANDMARK_PSIG[tsize];
                        PsiGi = tst.data.options.PsiGS1;
                        break;
                    case 2:
                        tst.data.options.PsiGS2 = new FLANDMARK_PSIG[tsize];
                        PsiGi = tst.data.options.PsiGS2;
                        break;
                }

                int temp = 0;
                for (int i = 0; i < tsize; i++)
                {
                    PsiGi[i] = new FLANDMARK_PSIG();

                    // disp ROWS
                    temp = breader.ReadInt32();

                    PsiGi[i].ROWS = temp;

                    // disp COLS
                    temp = breader.ReadInt32();

                    PsiGi[i].COLS = temp;

                    // disp
                    tmp_tsize = PsiGi[i].ROWS * PsiGi[i].COLS;

                    PsiGi[i].disp = new int[tmp_tsize];

                    for (int r = 0; r < tmp_tsize; r++)
                    {
                        PsiGi[i].disp[r] = breader.ReadInt32();
                    }
                }
            }

            fin.Dispose();
            breader.Dispose();

            tst.normalizedImageFrame = new byte[tst.data.options.bw[0] * tst.data.options.bw[1]];
            tst.bb = new double[4];
            tst.sf = new float[2];

            model = tst;
        }

        static int GetIndex(int r, int c, int nr)
        {
            return c * nr + r;
        }

        static int GetRow(int i, int r)
        {
            return (i - 1) % r;
        }

        static int GetCol(int i, int r)
        {
            return (i - 1) / r;
        }

        StringBuilder builder = new StringBuilder();
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

        public Point[] Detect(VMat m, int[] boundBox, int[] margin = null)
        {
            double[] landmarks;

            flandmark_detect(ref m, boundBox, ref model, out landmarks, margin);

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

        private void flandmark_detect(ref VMat img, int[] bbox, ref FLANDMARK_Model model, out double[] landmarks, int[] bw_margin = null)
        {
            landmarks = new double[model.data.options.M * 2];

            if (bw_margin != null)
            {
                model.data.options.bw_margin[0] = bw_margin[0];
                model.data.options.bw_margin[1] = bw_margin[1];
            }

            if (!flandmark_get_normalized_image_frame(ref img, bbox, ref model.bb, ref model.normalizedImageFrame, ref model))
            {
                landmarks = null;
                return;
            }
            
            flandmark_detect_base(model.normalizedImageFrame, model, landmarks);

            model.sf[0] = (float)(model.bb[2] - model.bb[0]) / model.data.options.bw[0];
            model.sf[1] = (float)(model.bb[3] - model.bb[1]) / model.data.options.bw[1];

            for (int i = 0; i < 2 * model.data.options.M; i += 2)
            {
                landmarks[i] = landmarks[i] * model.sf[0] + model.bb[0];
                landmarks[i + 1] = landmarks[i + 1] * model.sf[1] + model.bb[1];
            }
        }

        private bool flandmark_get_normalized_image_frame(ref VMat input, int[] bbox, ref double[] bb, ref byte[] face_img, ref FLANDMARK_Model model)
        {
            bool flag;
            int[] d = new int[2];
            double[] c = new double[2], nd = new double[2];

            // extend bbox by bw_margin
            d[0] = bbox[2] - bbox[0] + 1; d[1] = bbox[3] - bbox[1] + 1;
            c[0] = (bbox[2] + bbox[0]) / 2.0f; c[1] = (bbox[3] + bbox[1]) / 2.0f;
            nd[0] = d[0] * model.data.options.bw_margin[0] / 100.0f + d[0];
            nd[1] = d[1] * model.data.options.bw_margin[1] / 100.0f + d[1];

            bb[0] = (c[0] - nd[0] / 2.0f);
            bb[1] = (c[1] - nd[1] / 2.0f);
            bb[2] = (c[0] + nd[0] / 2.0f);
            bb[3] = (c[1] + nd[1] / 2.0f);

            flag = bb[0] > 0 && bb[1] > 0 && bb[2] < input.Width && bb[3] < input.Height
                && bbox[0] > 0 && bbox[1] > 0 && bbox[2] < input.Width && bbox[3] < input.Height;
            if (!flag)
                return false;

            Rect region = new Rect((int)bb[0], (int)bb[1], (int)bb[2] - (int)bb[0] + 1, (int)bb[3] - (int)bb[1] + 1);

            flag = input.Width <= 0 || input.Height <= 0 || region.Width <= 0 || region.Height <= 0;
            if (flag)
                return false;

            VMat resizedImage = VMat.New(input, region);

            resizedImage.Resize(new Size(model.data.options.bw[0], model.data.options.bw[1]), 0, 0, Inter);
            resizedImage.EqualizeHistogram();

            for (int x = 0; x < model.data.options.bw[0]; ++x)
            {
                for (int y = 0; y < model.data.options.bw[1]; ++y)
                {
                    face_img[GetIndex(y, x, model.data.options.bw[1])] = resizedImage.At<byte>(y, x);
                }
            }
            
            resizedImage.Dispose();

            return true;
        }

        FLANDMARK_PSI_SPARSE[] Cached_Psi_sparse;
        private void flandmark_detect_base(byte[] face_image, FLANDMARK_Model model, double[] landmarks)
        {
            int M = model.data.options.M;
            double[] W = model.W;
            int tsize = -1, cols = -1, rows = -1;
            int[] mapTable = model.data.mapTable;

            if (model.normalizedImageFrame == null)
            {
                model.normalizedImageFrame = face_image;
            }

            if(Cached_Psi_sparse == null)
            {
                Cached_Psi_sparse = new FLANDMARK_PSI_SPARSE[M];
            }

            Parallel.For(0, M, new ParallelOptions() { MaxDegreeOfParallelism=M }, (idx) =>
            {
                if (Cached_Psi_sparse[idx] == null)
                {
                    Cached_Psi_sparse[idx] = new FLANDMARK_PSI_SPARSE();
                }

                flandmark_get_psi_mat_sparse(ref Cached_Psi_sparse[idx], ref model, idx);
            });
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

                double[] q_temp = new double[tsize];
                Array.Copy(W, mapTable[GetIndex(idx, 0, M)] - 1, q_temp, 0, tsize);

                // sparse dot product <W_q, PSI_q>
                cols = (int)Cached_Psi_sparse[idx].PSI_COLS;
                rows = (int)Cached_Psi_sparse[idx].PSI_ROWS;
                uint[] psi_temp = Cached_Psi_sparse[idx].idxs;
                q[idx] = new double[cols];
                for (int i = 0; i < cols; ++i)
                {
                    double dotprod = 0.0f;
                    for (int j = 0; j < rows; ++j)
                    {
                        idx_qtemp = (int)psi_temp[(rows * i) + j];
                        dotprod += q_temp[idx_qtemp];
                    }
                    q[idx][i] = dotprod;
                }
                q_temp = null;

                if (idx > 0)
                {
                    tsize = mapTable[GetIndex(idx, 3, M)] - mapTable[GetIndex(idx, 2, M)] + 1;
                    g[idx - 1] = new double[tsize];
                    Array.Copy(W, mapTable[GetIndex(idx, 2, M)] - 1, g[idx - 1], 0, tsize);
                }
            }

            flandmark_argmax(landmarks, ref model.data.options, mapTable, Cached_Psi_sparse, q, g);

            g.Clear();
            q.Clear();
        }

        private void flandmark_get_psi_mat_sparse(ref FLANDMARK_PSI_SPARSE Psi, ref FLANDMARK_Model model, int lbpidx)
        {
            //uint[] Features;
            byte[] Images = model.normalizedImageFrame;
            uint im_H = (uint)model.data.imSize[0];
            uint im_W = (uint)model.data.imSize[1];
            uint[] Wins = model.data.lbp[lbpidx].wins;
            UInt16 win_H = (UInt16)model.data.lbp[lbpidx].winSize[0];
            UInt16 win_W = (UInt16)model.data.lbp[lbpidx].winSize[1];
            UInt16 nPyramids = model.data.lbp[lbpidx].hop;
            uint nDim = LibLBP.PyrGetDim(win_H, win_W, nPyramids) / 256;
            uint nData = model.data.lbp[lbpidx].WINS_COLS;

            uint cnt0, mirror, x, x1, y, y1, idx;
            uint[] win;

            if (Psi.idxs == null) // Psi.idxs.Length != nDim * nData)
            {
                Psi.idxs = new uint[nDim * nData];
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

                LibLBP.PyrFeaturesSparse(ref Psi.idxs, nDim, win, win_H, win_W, nDim * i);
            }

            Psi.PSI_COLS = nData;
            Psi.PSI_ROWS = nDim;
            //Psi.idxs = Features;
        }

        private void flandmark_argmax(double[] smax, ref FLANDMARK_Options options, int[] mapTable, FLANDMARK_PSI_SPARSE[] Psi_sparse, List<double[]> q, List<double[]> g)
        {
            byte M = options.M;

            int[] indices = new int[M];
            int tsize = mapTable[GetIndex(1, 3, M)] - mapTable[GetIndex(1, 2, M)] + 1;

            // left branch - store maximum and index of s5 for all positions of s1
            int q1_length = (int)Psi_sparse[1].PSI_COLS;

            double[] s1 = new double[2 * q1_length];
            double[] s1_maxs = new double[q1_length];

            for (int i = 0; i < q1_length; ++i)
            {
                // dot product <g_5, PsiGS1>
                flandmark_maximize_gdotprod(
                        //s2_maxs, s2_idxs,
                        ref s1[GetIndex(0, i, 2)], ref s1[GetIndex(1, i, 2)],
                        q[5], g[4], options.PsiGS1[GetIndex(i, 0, options.PSIG_ROWS[1])].disp,
                        options.PsiGS1[GetIndex(i, 0, options.PSIG_ROWS[1])].COLS, tsize
                        );
                s1[GetIndex(0, i, 2)] += q[1][i];
            }

            for (int i = 0; i < q1_length; ++i)
            {
                s1_maxs[i] = s1[GetIndex(0, i, 2)];
            }

            // right branch (s2->s6) - store maximum and index of s6 for all positions of s2
            int q2_length = (int)Psi_sparse[2].PSI_COLS;
            double[] s2 = new double[2 * q2_length];
            double[] s2_maxs = new double[q2_length];

            for (int i = 0; i < q2_length; ++i)
            {
                // dot product <g_6, PsiGS2>
                flandmark_maximize_gdotprod(
                        //s2_maxs, s2_idxs,
                        ref s2[GetIndex(0, i, 2)], ref s2[GetIndex(1, i, 2)],
                        q[6], g[5], options.PsiGS2[GetIndex(i, 0, options.PSIG_ROWS[2])].disp,
                        options.PsiGS2[GetIndex(i, 0, options.PSIG_ROWS[2])].COLS, tsize);
                s2[GetIndex(0, i, 2)] += q[2][i];
            }

            for (int i = 0; i < q2_length; ++i)
            {
                s2_maxs[i] = s2[GetIndex(0, i, 2)];
            }

            int q0_length = (int)Psi_sparse[0].PSI_COLS;
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
                        s1_maxs, g[0], options.PsiGS0[GetIndex(i, 0, options.PSIG_ROWS[0])].disp,
                        options.PsiGS0[GetIndex(i, 0, options.PSIG_ROWS[0])].COLS, tsize);
                s0[GetIndex(5, i, M)] = s1[GetIndex(1, (int)s0[GetIndex(1, i, M)], 2)];
                // q20
                maxq20 = -float.MaxValue;
                flandmark_maximize_gdotprod(
                        ref maxq20, ref s0[GetIndex(2, i, M)],
                        s2_maxs, g[1], options.PsiGS0[GetIndex(i, 1, options.PSIG_ROWS[0])].disp,
                        options.PsiGS0[GetIndex(i, 1, options.PSIG_ROWS[0])].COLS, tsize);
                s0[GetIndex(6, i, M)] = s2[GetIndex(1, (int)s0[GetIndex(2, i, M)], 2)];
                // q30
                maxq30 = -float.MaxValue;
                flandmark_maximize_gdotprod(
                        ref maxq30, ref s0[GetIndex(3, i, M)],
                        q[3], g[2], options.PsiGS0[GetIndex(i, 2, options.PSIG_ROWS[0])].disp,
                        options.PsiGS0[GetIndex(i, 2, options.PSIG_ROWS[0])].COLS, tsize);
                // q40
                maxq40 = -float.MaxValue;
                flandmark_maximize_gdotprod(
                        ref maxq40, ref s0[GetIndex(4, i, M)],
                        q[4], g[3], options.PsiGS0[GetIndex(i, 3, options.PSIG_ROWS[0])].disp,
                        options.PsiGS0[GetIndex(i, 3, options.PSIG_ROWS[0])].COLS, tsize);
                // q70
                maxq70 = -float.MaxValue;
                flandmark_maximize_gdotprod(
                        ref maxq70, ref s0[GetIndex(7, i, M)],
                        q[7], g[6], options.PsiGS0[GetIndex(i, 4, options.PSIG_ROWS[0])].disp,
                        options.PsiGS0[GetIndex(i, 4, options.PSIG_ROWS[0])].COLS, tsize);
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
            maximum = -float.MaxValue;
            idx = -1;
            for (int dp_i = 0; dp_i < cols; ++dp_i)
            {
                double dotprod = 0.0f;
                for (int dp_j = 0; dp_j < tsize; ++dp_j)
                {
                    dotprod += second[dp_j] * (double)(third[dp_i * tsize + dp_j]);
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
