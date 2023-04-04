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
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Wpf;

namespace OpenTKSlicingModule
{
    /// <summary>
    /// Interaction logic for VolumeViewer.xaml
    /// </summary>
    public partial class VolumeViewer : UserControl
    {
        private int _mapWidth = 1;
        public int MapWidth
        {
            get => _mapWidth;
            set { 
                _mapWidth = value;
                CreateScene();
            }
        }
        private int _mapHeight = 1;
        public int MapHeight
        {
            get => _mapHeight;
            set
            {
                _mapHeight = value;
                CreateScene();
            }
        }
        private int _mapDepth = 1;
        public int MapDepth
        {
            get => _mapDepth;
            set
            {
                _mapDepth = value;
                CreateScene();
            }
        }

        private bool _isOrtho = true;
        public bool IsOrthographic
        {
            get => _isOrtho;
            set
            {
                _isOrtho = value;
                scene.SetOrtho(value);
            }
        }

        
        // Dependency properties for future
        /*public int MapWidth
        {
            get { return (int)GetValue(MapWidthProperty); }
            set { SetValue(MapWidthProperty, value); }
        }

        public static readonly DependencyProperty MapWidthProperty =
            DependencyProperty.Register("MapWidth", typeof(int), typeof(VolumeViewer));

        public int MapHeight
        {
            get { return (int)GetValue(MapHeightProperty); }
            set { SetValue(MapHeightProperty, value); }
        }

        public static readonly DependencyProperty MapHeightProperty =
            DependencyProperty.Register("MapHeight", typeof(int), typeof(VolumeViewer));

        public int MapDepth
        {
            get { return (int)GetValue(MapDepthProperty); }
            set { SetValue(MapDepthProperty, value); }
        }

        public static readonly DependencyProperty MapDepthProperty =
            DependencyProperty.Register("MapDepth", typeof(int), typeof(VolumeViewer));*/

        private Scene scene;

        /// <summary>
        /// Setuping OpenGL for GlWPFControl
        /// </summary>
        public VolumeViewer()
        {
            InitializeComponent();

            var settings = new GLWpfControlSettings
            {
                MajorVersion = 4,
                MinorVersion = 6
            };
            OpenTkControl.Start(settings);
            
            scene = new Scene(MapWidth, MapHeight, MapDepth, Convert.ToInt32(OpenTkControl.ActualWidth), Convert.ToInt32(OpenTkControl.ActualHeight), IsOrthographic);
        }

        /// <summary>
        /// Render handler for OpenTK control. Calls scene to make a scene.
        /// </summary>
        /// <param name="delta"></param>
        private void OpenTkControl_OnRender(TimeSpan delta)
        {
            scene.Render(MapWidth, MapHeight, MapDepth);
        }

        /// <summary>
        /// Call resize event for OpenTK control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenTkControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            scene.Resize(Convert.ToInt32(OpenTkControl.ActualWidth), Convert.ToInt32(OpenTkControl.ActualHeight));
        }

        /// <summary>
        /// Handler for mouse move event. If left MB is pressed change viewing angle, if right MB is 
        /// pressed add offset to the spectating area to pan the view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenTkControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Vector2 end;
                end.X = Convert.ToSingle(e.GetPosition(this).X);
                end.Y = Convert.ToSingle(e.GetPosition(this).Y);
                scene.MouseMoveRot(end);
                double dist = Convert.ToDouble(scene.GetDiagonalLength());
                double percent = 0;
                if(SliceDepthSlider.Maximum != 0) percent = Math.Min(SliceDepthSlider.Value/SliceDepthSlider.Maximum, 1);
                SliceDepthSlider.Maximum = dist;
                SliceDepthSlider.Minimum = -dist;
                SliceDepthSlider.Value = Math.Round(SliceDepthSlider.Maximum * percent);
            }
            if (e.RightButton == MouseButtonState.Pressed)
            {
                Vector2 end;
                end.X = Convert.ToSingle(e.GetPosition(this).X);
                end.Y = Convert.ToSingle(e.GetPosition(this).Y);
                scene.MouseMovePan(end);
            }
        }

        /// <summary>
        /// Start capturing mouse movement for MouseMove event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenTkControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Vector2 start;
                start.X = Convert.ToSingle(e.GetPosition(this).X);
                start.Y = Convert.ToSingle(e.GetPosition(this).Y);
                scene.MouseDownRot(start);
            }

            if (e.ChangedButton == MouseButton.Right)
            {
                Vector2 start;
                start.X = Convert.ToSingle(e.GetPosition(this).X);
                start.Y = Convert.ToSingle(e.GetPosition(this).Y);
                scene.MouseDownPan(start);
            }
        }

        /// <summary>
        /// If leftCtrl is pressed change the zoom from scene class. If shift/some 
        /// other keys are pressed change the Slice depth slider's value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenTkControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (e.Delta > 0) scene.Zoom(1);
                else if (e.Delta < 0) scene.Zoom(-1);
            }
            else if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                if (e.Delta > 0) SliceDepthSlider.Value -= 10;
                else if (e.Delta < 0) SliceDepthSlider.Value += 10; ;
            }
            else
            {
                if (e.Delta > 0) SliceDepthSlider.Value -= 1;
                else if (e.Delta < 0) SliceDepthSlider.Value += 1;
            }
            string text = (scene.GetZoomLevel()).ToString("0");
            ZoomText.Text = "Zoom: " + text + "%";
        }

        /// <summary>
        /// Event handler for slice depth slider's value change. Calls scene to move slice in according to the
        /// angle that the user is viewing the data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SliceDepthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            scene.MoveSlice(Convert.ToInt32(SliceDepthSlider.Value), MapWidth, MapHeight, MapDepth);
        }

        /// <summary>
        /// Key down event handlers. Z calls scene to reset offset.
        /// X calls Slice depth slider to reset its value to zero.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenTkControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Z)) scene.ResetOffset();
            if (Keyboard.IsKeyDown(Key.X)) SliceDepthSlider.Value = 0;
        }

        /// <summary>
        /// When the control is ready to use, call create scene to make a new scene.
        /// </summary>
        private void OpenTkControl_Ready()
        {
            CreateScene();
        }

        /// <summary>
        /// Creates new scene and sets the slice depth slider values accordingly
        /// </summary>
        private void CreateScene() {
            scene = new Scene(MapWidth, MapHeight, MapDepth, Convert.ToInt32(OpenTkControl.ActualWidth), Convert.ToInt32(OpenTkControl.ActualHeight), IsOrthographic);
            double dist = Convert.ToDouble(scene.GetDiagonalLength());
            SliceDepthSlider.Maximum = dist;
            SliceDepthSlider.Minimum = -dist;
        }
    }
}
