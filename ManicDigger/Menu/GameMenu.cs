﻿using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using ManicDigger;
using System.Threading;
using System.Net;
using System.Xml;
using System.Windows.Forms;
using System.IO;
using ManicDigger.Menu;

namespace GameMenu
{
    public interface IForm
    {
        void Render();
        List<Widget> Widgets { get; set; }
    }
    public class Widget
    {
        public string Text;
        public RectangleF Rect;
        public string BackgroundImage;
        public string BackgroundImageSelected;
        public System.Threading.ThreadStart Click;
        public float FontSize = 24;
        public bool selected;
        public bool IsTextbox;
        public bool IsNumeric;
        public ThreadStart OnText;
        public bool Visible = true;
        public bool IsPassword = false;
        public Color TextColor = Color.White;
    }
    public partial class MenuWindow : IMyGameWindow
    {
        public MainGameWindow mainwindow;
        public IGameExit exit;
        public void OnLoad(EventArgs e)
        {
            mainwindow.VSync = VSyncMode.On;
            mainwindow.WindowState = WindowState.Normal;
            FormMainMenu();
            mainwindow.Keyboard.KeyDown += new EventHandler<KeyboardKeyEventArgs>(Keyboard_KeyDown);
        }
        public The3d the3d;
        public FormMainMenu formMainMenu;
        public FormJoinMultiplayer formJoinMultiplayer;
        public FormLogin formLogin;
        public FormSelectWorld formSelectWorld;
        public FormWorldOptions formWorldOptions;
        public FormMessageBox formMessageBox;
        public Game game;
        public ManicDigger.TextRenderer textrenderer;
        IForm currentForm;
        public int typingfield = -1;
        public ThreadStart OnFinishedTyping;
        void Keyboard_KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                typingfield = -1;
            }
        }
        public void OnKeyPress(OpenTK.KeyPressEventArgs e)
        {
            if (typingfield != -1)
            {
                var widget = currentForm.Widgets[typingfield];
                if (e.KeyChar == 8 && widget.Text.Length > 0)//backspace
                {
                    widget.Text = widget.Text.Substring(0, widget.Text.Length - 1);
                }
                else
                {
                    if (e.KeyChar == 8)
                    {
                        return;
                    }
                    if (widget.IsNumeric && !char.IsDigit(e.KeyChar))
                    {
                        return;
                    }
                    if (e.KeyChar == 22)
                    {
                        if (Clipboard.ContainsText())
                        {
                            widget.Text += Clipboard.GetText();
                        }
                        return;
                    }
                    widget.Text += e.KeyChar;
                }
                if (widget.OnText != null)
                {
                    widget.OnText();
                }
            }
            if (currentform != null)
            {
                currentform();
            }
            //..base.OnKeyPress(e);
        }
        //string typingbuffer = "";
        ThreadStart currentform;
        enum MainMenuState
        {
            Main,
            SinglePlayerSelectWorld,
        }
        public void OnResize(EventArgs e)
        {
            ResizeGraphics();
            //..base.OnResize(e);
        }
        void ResizeGraphics()
        {
            // Get new window size
            int width = mainwindow.Width;
            int height = mainwindow.Height;
            float aspect = (float)width / height;

            // Adjust graphics to window size
            GL.Viewport(0, 0, width, height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            //GLU.Perspective(45.0, aspect, 1.0, 100.0);
            GL.MatrixMode(MatrixMode.Modelview);
        }
        public void OnUpdateFrame(FrameEventArgs e)
        {
            //..base.OnUpdateFrame(e);
            if (mainwindow.Keyboard[Key.Escape])
            {
                mainwindow.Exit();
            }
        }
        public void OnRenderFrame(FrameEventArgs e)
        {
            //..base.OnRenderFrame(e);
            GL.ClearColor(Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            OrthoMode();
            //background
            UpdateMouse();
            DrawWidgets(currentForm);
            if (formMessageBox.Visible)
            {
                DrawWidgets(formMessageBox);
            }
            else
            {
                currentForm.Render();
            }
            //PerspectiveMode();
            try
            {
                mainwindow.SwapBuffers();
            }
            catch { Application.Exit(); } //"failed to swap buffers" crash when exiting program.
        }
        bool mouseleftclick = false;
        bool mouseleftdeclick = false;
        bool wasmouseleft = false;
        bool mouserightclick = false;
        bool mouserightdeclick = false;
        bool wasmouseright = false;
        private void UpdateMouse()
        {
            if (!mainwindow.Focused)
            {
                return;
            }
            mouseleftclick = (!wasmouseleft) && Mouse[OpenTK.Input.MouseButton.Left];
            mouserightclick = (!wasmouseright) && Mouse[OpenTK.Input.MouseButton.Right];
            mouseleftdeclick = wasmouseleft && (!Mouse[OpenTK.Input.MouseButton.Left]);
            mouserightdeclick = wasmouseright && (!Mouse[OpenTK.Input.MouseButton.Right]);
            wasmouseleft = Mouse[OpenTK.Input.MouseButton.Left];
            wasmouseright = Mouse[OpenTK.Input.MouseButton.Right];

            if (formMessageBox.Visible)
            {
                UpdateWidgetsMouse(formMessageBox);
            }
            else
            {
                UpdateWidgetsMouse(currentForm);
            }
        }
        private void UpdateWidgetsMouse(IForm form)
        {
            selectedWidget = null;
            for (int i = 0; i < form.Widgets.Count; i++)
            {
                Widget b = form.Widgets[i];
                if (b.Rect.Contains(((float)Mouse.X / mainwindow.Width) * ConstWidth, ((float)Mouse.Y / mainwindow.Height) * ConstHeight))
                {
                    selectedWidget = i;
                }
            }
            if (mouseleftclick && selectedWidget != null)
            {
                var w = form.Widgets[selectedWidget.Value];
                if (w.Click != null)
                {
                    w.Click();
                }
                if (w.IsTextbox)
                {
                    typingfield = selectedWidget.Value;
                }
            }
        }
        void DrawWidgets(IForm form)
        {
            for (int i = 0; i < form.Widgets.Count; i++)
            {
                Widget b = form.Widgets[i];
                if (!b.Visible)
                {
                    continue;
                }
                string img = ((selectedWidget == i || b.selected)
                    && b.BackgroundImageSelected != null)
                    ? b.BackgroundImageSelected : b.BackgroundImage;
                if (img != null)
                {
                    the3d.Draw2dBitmapFile(img, b.Rect.X, b.Rect.Y, b.Rect.Width, b.Rect.Height);
                }
                if (b.Text != null)
                {
                    int dx = b.FontSize > 20 ? 49 : 20;
                    string text = b.IsPassword ? PassString(b.Text) : b.Text;
                    if (typingfield == i)
                    {
                        text += "&7|";
                    }
                    the3d.Draw2dText(text, b.Rect.X + dx, b.Rect.Y + dx, b.FontSize,
                        (b.BackgroundImage == null && b.selected) ? Color.Red : b.TextColor);
                }
            }
        }
        string PassString(string s)
        {
            string ss = "";
            for (int i = 0; i < s.Length; i++)
            {
                ss += "*";
            }
            return ss;
        }
        public void AddCaption(IForm form, string text)
        {
            form.Widgets.Add(new Widget() { Text = text, FontSize = 48, Rect = new RectangleF(ConstWidth / 2 - 430 * 1.5f / 2, 10, 1024 * 1.5f, 512 * 1.5f) });
        }
        public void AddBackground(List<Widget> widgets)
        {
            //for (int x = 0; x < ConstWidth / 64; x++)
            {
                widgets.Add(new Widget() { BackgroundImage = Path.Combine("gui", "background.png"), Rect = new RectangleF(0, 0, 2048, 2048) });
            }
        }
        public string button4 = Path.Combine("gui", "button4.png");
        public string button4sel = Path.Combine("gui", "button4_sel.png");
        public void AddOkCancel(IForm form, ThreadStart ok, ThreadStart cancel)
        {
            form.Widgets.Add(new Widget()
            {
                BackgroundImage = button4,
                BackgroundImageSelected = button4sel,
                Rect = new RectangleF(350, 1000, 400, 128),
                Text = "OK",
                Click = ok,
            });
            form.Widgets.Add(new Widget()
            {
                BackgroundImage = button4,
                BackgroundImageSelected = button4sel,
                Rect = new RectangleF(850, 1000, 400, 128),
                Text = "Cancel",
                Click = cancel,
            });
        }
        public void MessageBoxYesNo(string text, ThreadStart yes, ThreadStart no)
        {
            formMessageBox.MessageBoxYesNo(text,
                delegate { yes(); formMessageBox.Visible = false; },
                delegate { no(); formMessageBox.Visible = false; });
            formMessageBox.Visible = true;
        }
        List<Widget> optionswidgets = new List<Widget>();
        void FormGameOptionsGraphics()
        {
            gameoptionstype = 0;
            FormGameOptions();
        }
        void FormGameOptionsKeys()
        {
            gameoptionstype = 1;
            FormGameOptions();
        }
        void FormGameOptionsOther()
        {
            gameoptionstype = 2;
            FormGameOptions();
        }
        public class Options
        {
            public bool Shadows;
            public int Font;
            public int DrawDistance = 256;
            public bool UseServerTextures = true;
            public bool EnableSound = true;
            public SerializableDictionary<int, int> Keys = new SerializableDictionary<int, int>();
        }
        Options options = new Options();
        //List<Button> widgets = new List<Button>();
        int? selectedWidget;
        public int ConstWidth = 1600;
        public int ConstHeight = 1200;
        public int[] drawDistances = { 32, 64, 128, 256, 512 };
        private void ToggleFog()
        {
            for (int i = 0; i < drawDistances.Length; i++)
            {
                if (options.DrawDistance == drawDistances[i])
                {
                    options.DrawDistance = drawDistances[(i + 1) % drawDistances.Length];
                    return;
                }
            }
            options.DrawDistance = drawDistances[0];
        }
        void OrthoMode()
        {
            //GL.Disable(EnableCap.DepthTest);
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Ortho(0, ConstWidth, ConstHeight, 0, 0, 1);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();
        }
        // Set Up A Perspective View
        void PerspectiveMode()
        {
            // Enter into our projection matrix mode
            GL.MatrixMode(MatrixMode.Projection);
            // Pop off the last matrix pushed on when in projection mode (Get rid of ortho mode)
            GL.PopMatrix();
            // Go back to our model view matrix like normal
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix();
            //GL.LoadIdentity();
            //GL.Enable(EnableCap.DepthTest);
        }
        public void FormMainMenu()
        {
            currentForm = formMainMenu;
        }
        public void FormSelectWorld()
        {
            afterSelectWorld = delegate
            {
                int id = formSelectWorld.selectedWorld.Value;
                if (string.IsNullOrEmpty(game.GetWorlds()[id]))
                {
                    FormWorldOptions(id);
                    afterWorldOptions = delegate
                    {
                        game.StartSinglePlayer(id);
                    };
                }
                else
                {
                    game.StartSinglePlayer(id);
                }
            };
            currentForm = formSelectWorld;
        }
        private void FormWorldOptions(int id)
        {
            currentForm = formWorldOptions;
            formWorldOptions.worldId = id;
            formWorldOptions.Initialize(); //after worldId set.
        }
        public void FormJoinMultiplayer()
        {
            currentForm = formJoinMultiplayer;
        }
        public void FormLogin()
        {
            currentForm = formLogin;
        }
        public ThreadStart afterSelectWorld = delegate { };
        public ThreadStart afterWorldOptions = delegate { };
        public void OnFocusedChanged(EventArgs e)
        {
        }
        public void OnClosed(EventArgs e)
        {
        }
        public void Exit()
        {
            mainwindow.Exit();
            exit.exit = true;
        }
        public OpenTK.Input.KeyboardDevice Keyboard { get { return mainwindow.Keyboard; } }
        public OpenTK.Input.MouseDevice Mouse { get { return mainwindow.Mouse; } }
    } 
}
