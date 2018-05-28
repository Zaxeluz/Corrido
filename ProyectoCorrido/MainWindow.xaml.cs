using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio;
using NAudio.Wave;
using NAudio.Dsp;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;

namespace ProyectoCorrido
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WaveIn waveIn;
        WaveFormat formato;
        WaveOutEvent waveOut;
        WaveFileWriter writer;


        private Mp3FileReader reader;
        private WaveOutEvent output;
        DispatcherTimer timer;

        int tiempoTranscurrido = 0;

        int[] notas = { 0, 1, 2, 3, 4, 5, 6 };

        public MainWindow()
        {

            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += OnTimerTick;
        }

        private void OnTimerTick(object sender, EventArgs e)
        {


            if (reader != null)
            {
                string tiempoActual = reader.CurrentTime.ToString();
                tiempoActual = tiempoActual.Substring(0, 8);
                txtTimer.Text = tiempoActual;
            }

            lblTiempoTranscurrido.Text = tiempoTranscurrido.ToString();


            tiempoTranscurrido++;


            moverSlider();


        }

        void moverSlider()
        {
            if (tiempoTranscurrido == 96)
            {
                sldPC.Value = 50;
            }
            if (tiempoTranscurrido == 21)
            {
                sldPC.Value = 50;
            }
            if (tiempoTranscurrido == 25)
            {
                sldPC.Value = 0;
            }
            if (tiempoTranscurrido == 28)
            {
                sldPC.Value = 20;
            }
        }




        private void lblPlay_Click_1(object sender, RoutedEventArgs e)
        {
            waveIn = new WaveIn();
            waveIn.WaveFormat = new WaveFormat(44100, 16, 1);
            formato = waveIn.WaveFormat;



            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;

            writer =
                new WaveFileWriter("sonido2.wav", formato);

            waveIn.StartRecording();

            if (output != null && output.PlaybackState == PlaybackState.Paused)
            {
                output.Play();
            }
            else
            {
                output = new WaveOutEvent();
                output.PlaybackStopped += OnPlaybackStop;
                reader = new Mp3FileReader("Karma.mp3");


                //Configuraciones de WaveOut
                output.NumberOfBuffers = 2;
                output.DesiredLatency = 150;

                output.Init(reader);
                output.Play();
                timer.Start();
            }
        }

        void OnDataAvailable(object sender, WaveInEventArgs e)
        {

            byte[] buffer = e.Buffer;
            int bytesGrabados = e.BytesRecorded;


            double numMuestras = bytesGrabados / 2;
            int exponente = 1;
            int numeroMuestrasComplejas = 0;
            int bitsMaximos = 0;

            do //1,200
            {
                bitsMaximos = (int)Math.Pow(2, exponente);
                exponente++;
            } while (bitsMaximos < numMuestras);

            //bitsMaximos = 2048
            //exponente = 12

            //numeroMuestrasComplejas = 1024
            //exponente = 10

            exponente -= 2;
            numeroMuestrasComplejas = bitsMaximos / 2;

            Complex[] muestrasComplejas =
                new Complex[numeroMuestrasComplejas];

            for (int i = 0; i < bytesGrabados; i += 2)
            {
                // byte i =  0 1 1 0 0 1 1 1
                //byte i+1 = 0 0 0 0 0 0 0 0 0 1 1 0 0 1 1 1
                // or      = 0 1 1 0 0 1 1 1 0 1 1 0 0 1 1 1
                short muestra =
                        (short)Math.Abs((buffer[i + 1] << 8) | buffer[i]);
                //lblMuestra.Text = muestra.ToString();
                //sldVolumen.Value = (double)muestra;

                float muestra32bits = (float)muestra / 32768.0f;

                if (i / 2 < numeroMuestrasComplejas)
                {
                    muestrasComplejas[i / 2].X = muestra32bits;
                }
                //acumulador += muestra;
                //numMuestras++;
            }
            //double promedio = acumulador / numMuestras;
            //sldVolumen.Value = promedio;
            //writer.Write(buffer, 0, bytesGrabados);

            FastFourierTransform.FFT(true, exponente, muestrasComplejas);
            float[] valoresAbsolutos =
                new float[muestrasComplejas.Length];
            for (int i = 0; i < muestrasComplejas.Length; i++)
            {
                valoresAbsolutos[i] = (float)
                    Math.Sqrt((muestrasComplejas[i].X * muestrasComplejas[i].X) +
                    (muestrasComplejas[i].Y * muestrasComplejas[i].Y));

            }

            int indiceMaximo =
                valoresAbsolutos.ToList().IndexOf(
                    valoresAbsolutos.Max());

            float frecuenciaFundamental =
                (float)(indiceMaximo * waveIn.WaveFormat.SampleRate) / (float)valoresAbsolutos.Length;

            lblFrecuencia.Text = frecuenciaFundamental.ToString();
            if (frecuenciaFundamental >= 80 && frecuenciaFundamental <= 120)
            {
                if (waveOut != null && reader != null)
                {
                    if (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        waveOut.Stop();
                    }
                }
            }
            
        }

        void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            writer.Dispose();

        }
        private void OnPlaybackStop(object sender, StoppedEventArgs e)
        {
            reader.Dispose();
            output.Dispose();
            timer.Stop();
        }

        private void sldPC_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
        }
    }
}