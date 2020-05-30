using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
//
//
//
namespace MapaCalor
{

    public partial class Form1 : Form
    {
        Bitmap bmpHeatMap;
        Bitmap bmpHeatScale;

        private struct Pto
        {
            public int x;
            public int y;
            public float Valor;
        }
        List<Pto> Ptos = new List<Pto>();


        HeatColor2 hc;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Define pontos de temperatura fixa no plano
            Ptos.Add(new Pto { x =   0, y =   0, Valor =  0f });
            Ptos.Add(new Pto { x = 200, y =   0, Valor =  0f });
            Ptos.Add(new Pto { x =   0, y = 200, Valor =  0f });
            Ptos.Add(new Pto { x = 200, y = 200, Valor =  0f });
            Ptos.Add(new Pto { x =  66, y =  66, Valor = 10f });
            Ptos.Add(new Pto { x = 133, y = 133, Valor =-10f });

            //Cria um bitmap em memória para otimizar o Paint da picturebox
            bmpHeatMap = new Bitmap(pictureBox1.ClientRectangle.Width, pictureBox1.ClientRectangle.Height,System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            //Cria um bitmap para a escala de cores
            bmpHeatScale = new Bitmap(pictureBox2.ClientRectangle.Width, pictureBox2.ClientRectangle.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            //Desenha o mapa de calor no Bitmap
            DrawHeatMap(bmpHeatMap, bmpHeatScale);
        }

        private void DrawHeatMap(Bitmap bitmap, Bitmap bitmapScale)
        {
            //Divide a área de desenho em células retangulares
            int StepX = bitmap.Width / 80;
            int StepY = bitmap.Height / 80;

            //Determina os limites de escala
            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (Pto p in Ptos)
            {
                if (p.Valor > max) max = p.Valor;
                if (p.Valor < min) min = p.Valor;
            }
            label1.Text = min.ToString();
            label2.Text = ((max+min)/2).ToString();
            label3.Text = max.ToString();



            //Define a escala de cores para os limites de escala
            hc = new HeatColor2(min, max, Color.Green, Color.Yellow, Color.Red);

            //Cria o objeto Graphics direcionado ao Bitmap
            Graphics graphicsObj = Graphics.FromImage(bitmap);
            graphicsObj.Clear(Color.White);

            //Cria um pincel (inicializa com uma cor qualquer)
            SolidBrush myBrush = new SolidBrush(Color.Green);

            //Plota pontos de temperatura fixa:
            foreach (Pto p in Ptos)
            {
                //Acerta a cor do pincel em funçaõ do valor do ponto
                myBrush.Color = hc.GetColor(p.Valor);

                //Plota um retangulo na posição do do ponto com a cor correspondente ao seu valor
                graphicsObj.FillRectangle(myBrush, p.x, p.y, StepX, StepY);
            }

            //Criação do Mapa de Calor:
            //Faz uma varredura no bitmap, calculando os valores de cada ponto através de uma equação do tipo Campo elétrico
            for (int x = 0; x < bitmap.Width; x += StepX)
            {
                for (int y = 0; y < bitmap.Height; y += StepY)
                {
                    float numerador = 0;
                    float denominador = 0;
                    bool pula = false;
                    foreach (Pto p in Ptos)
                    {
                        if (p.x == x && p.y == y)
                        {
                          pula = true;
                          continue; //Salta os pontos com distância nula porque são os que já foram plotados
                        }
                        float d = (float)Math.Sqrt((x - p.x) * (x - p.x) + (y - p.y) * (y - p.y));
                        d = d * d;
                        if (d == 0) continue; 
                        float dinv = 1 / d;
                        numerador += p.Valor * dinv;
                        denominador += dinv;
                    }
                    if (pula) continue;
                    float val = numerador / denominador;
                    myBrush.Color = hc.GetColor(val);
                    graphicsObj.FillRectangle(myBrush, x, y, StepX, StepY);
                }
            }

            graphicsObj.Dispose();
            myBrush.Dispose();



            //Desenha a escala

            //Redireciona o objeto Graphics para o bitma da escala
            graphicsObj = Graphics.FromImage(bitmapScale);
            graphicsObj.Clear(Color.White);

            //Prepara a caneta:
            Pen pen = new Pen(Color.Black,8);

            //
            float Divisor = 100;
            float passo = (max - min) / Divisor;
            float X = 0, XAnt = 0;
            float passoX = bitmapScale.Width / Divisor;
            for (float v = min; v < max; v += passo)
            {
                X += passoX;
                pen.Color = hc.GetColor(v);
                graphicsObj.DrawLine(pen, XAnt, bitmapScale.Height / 2, X, bitmapScale.Height / 2);
                XAnt = X;
            }

            graphicsObj.Dispose();
            myBrush.Dispose();

        }

        //Quando for necessário redesenhar a picturebox...
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(bmpHeatMap, 0, 0, bmpHeatMap.Width, bmpHeatMap.Height);
        }

        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(bmpHeatScale, 0, 0, bmpHeatScale.Width, bmpHeatScale.Height);
        }
    }


    //-------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Cria uma escala de cores correspondente a uma escala de valores
    /// </summary>
    public class HeatColor
    {
        byte Rmax, Rmin, Gmax, Gmin, Bmax, Bmin;
        int DeltaR, DeltaG, DeltaB;
        float EscMax, EscMin, DeltaEsc;

        //Construtor
        /// <summary>
        /// Construtor
        /// </summary>
        /// <param name="EscMin">Valor de escala mínima</param>
        /// <param name="EscMax">Valor de escala máxima</param>
        /// <param name="CorMin">Cor correspondente ao valor de escala mínima</param>
        /// <param name="CorMax">Cor correspondente ao valor de escala máxima</param>
        public HeatColor(float EscMin, float EscMax, Color CorMin, Color CorMax)
        {
            this.EscMax = EscMax;
            this.EscMin = EscMin;
            DeltaEsc = EscMax - EscMin;

            Rmax = CorMax.R;
            Gmax = CorMax.G;
            Bmax = CorMax.B;

            Rmin = CorMin.R;
            Gmin = CorMin.G;
            Bmin = CorMin.B;

            DeltaR = Rmax - Rmin;
            DeltaG = Gmax - Gmin;
            DeltaB = Bmax - Bmin;
        }
        /// <summary>
        /// Retorna uma cor para Val dentro da escala
        /// </summary>
        /// <param name="Val">Valor para o aqual será atribuida uma cor</param>
        /// <returns></returns>
        public Color GetColor(float Val)
        {
            //Satura nos extremos:
            if (Val > EscMax) Val = EscMax;
            if (Val < EscMin) Val = EscMin;

            //Interpola as componentes R, G e B
            float fator = (Val - EscMin) / DeltaEsc;
            byte R = Convert.ToByte((DeltaR * fator) + Rmin) ;
            byte G = Convert.ToByte((DeltaG * fator) + Gmin);
            byte B = Convert.ToByte((DeltaB * fator) + Bmin);
            return Color.FromArgb(R, G, B);
        }
    }

    //---------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Cria uma escala de cores correspondente a uma escala de valores.
    /// A escala de cores passa por 3 cores fornecidas - CorMin, CorMed e Cor Max
    /// </summary>
    public class HeatColor2
    {
        HeatColor hc1; //Primeira metade da escala de cores
        HeatColor hc2; //Segunda metade da escala de cores
        float EscMed;
        /// <summary>
        /// Construtor
        /// </summary>
        /// <param name="EscMin">Valor de escala mínima</param>
        /// <param name="EscMax">Valor de escala máxima</param>
        /// <param name="CorMin">Cor correspondente ao valor de escala mínima</param>
        /// <param name="CorMed">Cor correspondente ao valor médio da escala</param>
        /// <param name="CorMax">Cor correspondente ao valor de escala máxima</param>
        public HeatColor2(float EscMin, float EscMax, Color CorMin, Color CorMed, Color CorMax)
        {
            EscMed = (EscMax + EscMin) / 2; //Meio da escala
            hc1 = new HeatColor(EscMin, EscMed, CorMin, CorMed);
            hc2 = new HeatColor(EscMed, EscMax, CorMed, CorMax);
        }

        /// <summary>
        /// Retorna uma cor para Val dentro da escala
        /// </summary>
        /// <param name="Val">Valor para o aqual será atribuida uma cor</param>
        /// <returns></returns>
        public Color GetColor(float valor)
        {
            //Se o valor fornecido está na primeira metade da escala
            if (valor < EscMed) return hc1.GetColor(valor);

            //Se o valor fornecido está na segunda metade da escala
            else return hc2.GetColor(valor);
        }

    }
}
