using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Interaction;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace ProyectoKinectVer2
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor _sensor;  //El sensor que se va a utilizar
        private InteractionStream _interactionStream;

        private Skeleton[] _skeletons; //Arreglo para acceder al esqueleto 
        private UserInfo[] _userInfos; //Informacion de los usuarios

        //Diccionario de eventos de la mano derecha e isquierda
        private Dictionary<int, InteractionHandEventType> _lastLeftHandEvents = new Dictionary<int, InteractionHandEventType>();
        private Dictionary<int, InteractionHandEventType> _lastRightHandEvents = new Dictionary<int, InteractionHandEventType>();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
           
            //se consulta si se esta en modo de diseño ¿?
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
            // Como solo es un test solo se usa una kinect
            _sensor = KinectSensor.KinectSensors.FirstOrDefault();
            //Se valida de que exista el sensor kinect
            if (_sensor == null)
            {
                MessageBox.Show("No Kinect Sensor detected!");
                Close();
                return;
            }
            // se rellena el esqueleto
            _skeletons = new Skeleton[_sensor.SkeletonStream.FrameSkeletonArrayLength];
            //Y se coloca la cantidad de usuarios ¿?
            _userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];

            //Rango del sensor kinect aca se deja default.
            _sensor.DepthStream.Range = DepthRange.Near;
            //Resolucion y frames por segundo del sensor
            _sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

            //el tipo de esqueleto (Seated solo el torso y default todo el cuerpo aca debe ser default
            _sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
            //Rango del traking del esqueleto false;
            _sensor.SkeletonStream.EnableTrackingInNearRange = true;
            //se habilita el stream del esqueleto
            _sensor.SkeletonStream.Enable();


            _interactionStream = new InteractionStream(_sensor, new DummyInteractionClient());
            _interactionStream.InteractionFrameReady += InteractionStreamOnInteractionFrameReady;

            //Listener del esqueleto y vista de la kinect
            _sensor.DepthFrameReady += SensorOnDepthFrameReady;
            _sensor.SkeletonFrameReady += SensorOnSkeletonFrameReady;
            //Inicializar el sensor
            _sensor.Start();
        }

        //Listener del esqueleto
        private void SensorOnSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs skeletonFrameReadyEventArgs)
        {
            using (SkeletonFrame skeletonFrame = skeletonFrameReadyEventArgs.OpenSkeletonFrame())
            {
                //Si el eskeleto es null se sale del metodo
                if (skeletonFrame == null)
                {
                    return;
                }

                try
                {
                    //se le coloca el esqueleto al areglo
                    skeletonFrame.CopySkeletonDataTo(_skeletons);
                    var accelerometerReading = _sensor.AccelerometerGetCurrentReading();
                    _interactionStream.ProcessSkeleton(_skeletons, accelerometerReading, skeletonFrame.Timestamp);
                }
                catch (InvalidOperationException)
                {
                    // Error al obtener el esqueleto del sensor
                }
            }
        }
        //Listener de la imagen
        private void SensorOnDepthFrameReady(object sender, DepthImageFrameReadyEventArgs depthImageFrameReadyEventArgs)
        {
            using (DepthImageFrame depthFrame = depthImageFrameReadyEventArgs.OpenDepthImageFrame())
            {
                //Si la imagen es null se sale del metodo
                if (depthFrame == null)
                {
                    return;
                }
                try
                {
                    // pasan los pixceles al arreglo
                    _interactionStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);
                }
                catch (InvalidOperationException)
                {
                    // DepthFrame functions may throw when the sensor gets
                    // into a bad state.  Ignore the frame in that case.
                }
            }
        }
        //Listener de las interacciones del esqueleto 
        private void InteractionStreamOnInteractionFrameReady(object sender, InteractionFrameReadyEventArgs args)
        {
            using (var iaf = args.OpenInteractionFrame()) //dispose as soon as possible
            {
                if (iaf == null)
                {
                    return;
                }
                iaf.CopyInteractionDataTo(_userInfos);
            }

            StringBuilder dump = new StringBuilder();

            var hasUser = false;
            //Ciclo por los usuarios disponibles 
            foreach (var userInfo in _userInfos)
            {
                //obtiene id del usuario proporcionado por kinec
                var userID = userInfo.SkeletonTrackingId;
                //si el id es 0 se sigue con el siguiente
                if (userID == 0)
                {
                    continue;
                }
                //como existe usuario le coloca true
                hasUser = true;
                //se agrega el id del usuario al stringbuilder
                dump.AppendLine("User ID = " + userID);
                dump.AppendLine("  Hands: ");
                //Se crea un arreglo con las manos disponibles de la persona y se le asigna
                var hands = userInfo.HandPointers;
                if (hands.Count == 0)
                {

                    dump.AppendLine("    No hands");
                }
                else
                {
                    //se recorren las manos por persona
                    foreach (var hand in hands)
                    {
                        //Se diferencia mano derecha o izquierda
                        var lastHandEvents = hand.HandType == InteractionHandType.Left
                                                 ? _lastLeftHandEvents
                                                 : _lastRightHandEvents;

                        if (hand.HandEventType != InteractionHandEventType.None)
                        {
                            lastHandEvents[userID] = hand.HandEventType;
                        }
                        var lastHandEvent = lastHandEvents.ContainsKey(userID)
                                                ? lastHandEvents[userID]
                                                : InteractionHandEventType.None;
                        //se obtienen datos de las manos, como que tipo 
                        dump.AppendLine();
                        dump.AppendLine("    Mano derecha o izquierda : " + hand.HandType);
                        dump.AppendLine("    Tipo de evento de la mano: " + hand.HandEventType);
                        dump.AppendLine("    ultimo  evento: " + lastHandEvent);
                        dump.AppendLine("    esta activa: " + hand.IsActive);
                        dump.AppendLine("    es la mano dominante en el programa: " + hand.IsPrimaryForUser);
                        dump.AppendLine("    IsInteractive: " + hand.IsInteractive);
                        dump.AppendLine("    PressExtent: " + hand.PressExtent.ToString("N3"));
                        dump.AppendLine("    es precionada: " + hand.IsPressed);
                        dump.AppendLine("    es trackeada: " + hand.IsTracked);
                        dump.AppendLine("    X: " + hand.X.ToString("N3"));
                        dump.AppendLine("    Y: " + hand.Y.ToString("N3"));
                        dump.AppendLine("    RawX: " + hand.RawX.ToString("N3"));
                        dump.AppendLine("    RawY: " + hand.RawY.ToString("N3"));
                        dump.AppendLine("    RawZ: " + hand.RawZ.ToString("N3"));
                        movimiento(hand.X, hand.Y, hand.HandType);
                      
                        if (hand.HandEventType == InteractionHandEventType.Grip && hand.HandType == InteractionHandType.Right)
                        {
                            recDerecha.Fill = new SolidColorBrush(Colors.Red);
                        }
                        else if (hand.HandEventType == InteractionHandEventType.GripRelease && hand.HandType == InteractionHandType.Right)
                        {
                            recDerecha.Fill = new SolidColorBrush(Colors.Black);
                        }
                      
                    }
                }
               

                //Se imprimen las variables
               // tb.Text = dump.ToString();
            }

            // si no existe el usuario
            if (!hasUser)
            {
               // tb.Text = "No user detected.";
            }
        }

        private void movimiento(double x, double y, InteractionHandType interactionHandType)
        {
            double h = canvasContenedor.ActualHeight;
            double w = canvasContenedor.ActualWidth;

            alto.Text = "Alto : " + h;
            ancho.Text = "Ancho : " + w;
                       
            double posicionY =y * w;
            double posicionX = x * h;


            double posicionYEstatica = 0.0;
            double posicionXEstatica = 0.0;

            

            if (posicionY < 0)
            {
                posicionY = 0;
            }
            if (posicionX < 0)
            {
                posicionX = 0;
            }
            if (posicionX > (h-70))
            {
                posicionXEstatica = h-70;
                posicionX = h - 70;
            }
            if (posicionY > (w-70))
            {
                posicionYEstatica = h - 70;
                posicionY = w-70;
            }
            Posicionalto.Text = "Alto : " + posicionYEstatica;
            Posicionancho.Text = "Ancho : " + posicionXEstatica;
           
            if (interactionHandType == InteractionHandType.Left)
            {
                Canvas.SetTop(recIzquierda, posicionY);
                Canvas.SetLeft(recIzquierda, posicionX);                
            }
            else
            {
                Canvas.SetTop(recDerecha, posicionY);
                Canvas.SetLeft(recDerecha, posicionX);
            }
                          
        }
    }
}
