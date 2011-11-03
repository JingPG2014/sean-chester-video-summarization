﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.IO;
using System.Timers;
using System.Threading.Tasks;
using System.Media;
using System.ComponentModel;

namespace VideoPlayer
{
    public class Video: INotifyPropertyChanged
    {
        enum PlayerState
        {
            Playing,
            Paused,
            Stopped,
            Unstarted
        };

        enum Color : uint
        {
            RED = 0,
            GREEN,
            BLUE
        };

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private static System.Windows.Threading.DispatcherTimer VideoTimer;
        private byte[] fileImageBufferOne;
        private byte[] fileImageBufferTwo;
        private BitmapSource _frame;

        public uint VideoWidth { get; set; }
        public uint VideoHeight { get; set; }
        public BitmapSource Frame 
        {
            get
            {
                return _frame;
            }
            set
            {
                _frame = value;
                NotifyPropertyChanged("Frame");
            }
        }
        public String VideoPath { get; set; }
        public String AudioPath { get; set; }
        public uint OverallFrameCount { get; set; }

        private bool AudioOpened { get; set; }
        private SoundPlayer AudioPlayer { get; set; }
        private byte CurrentBuffer { get; set; }
        private bool VideoOpened { get; set; }
        private BinaryReader FileReader { get; set; }
        private uint FrameRate { get; set; }
        private uint SecondsToBuffer { get; set; }
        private PlayerState State { get; set; }
        
        public Video()
        {
            VideoHeight = 240;
            VideoWidth = 320;
            SecondsToBuffer = 6;
            FrameRate = 24;
            AudioOpened = false;
            VideoOpened = false;
            OverallFrameCount = 0;
            VideoTimer = new System.Windows.Threading.DispatcherTimer();
            VideoTimer.Interval = new TimeSpan(0, 0, 0, 0,(int) ( 1000 / FrameRate));
            VideoTimer.IsEnabled = false;
            VideoTimer.Tick += OnVideoTimerTick;
            State = PlayerState.Unstarted;
            CurrentBuffer = 0;

            AudioPlayer = new SoundPlayer();
            AudioPlayer.LoadCompleted += new System.ComponentModel.AsyncCompletedEventHandler((obj, args) => { AudioOpened = true; });
            fileImageBufferOne = new byte[VideoHeight * VideoWidth * 3 * FrameRate * SecondsToBuffer];
            fileImageBufferTwo = new byte[VideoHeight * VideoWidth * 3 * FrameRate * SecondsToBuffer];


            
            /// Binding Test
            //byte[] frameBuffer = new byte[VideoHeight * VideoWidth];

            //for (uint imageBufferStart = 0;
            //    imageBufferStart < VideoHeight * VideoWidth;
            //    ++imageBufferStart)
            //{
            //    frameBuffer[imageBufferStart] = 111;
            //}

            //Frame = BitmapSource.Create((int)VideoWidth,
            //                            (int)VideoHeight,
            //                            96,
            //                            96,
            //                            System.Windows.Media.PixelFormats.Gray8,
            //                            null,
            //                            frameBuffer,
            //                            (int)VideoWidth);

        }

        private void OnVideoTimerTick(object source, EventArgs e)
        {
            if (State == PlayerState.Playing)
            {
                if (OverallFrameCount % (SecondsToBuffer * FrameRate) != 0 || OverallFrameCount == 0)
                {
                    //Display image
                    if (CurrentBuffer == 0)
                    {
                        GenerateFrame(fileImageBufferOne);
                    }
                    else
                    {
                        GenerateFrame(fileImageBufferTwo);
                    }                 
                }
                else
                {
                    if (CurrentBuffer == 0)
                    {
                        Task.Factory.StartNew(() => { LoadBuffer(fileImageBufferOne); });
                        GenerateFrame(fileImageBufferTwo);
                        CurrentBuffer = (byte)1;
                    }
                    else
                    {
                        Task.Factory.StartNew(() => { LoadBuffer(fileImageBufferTwo); });
                        GenerateFrame(fileImageBufferOne);
                        CurrentBuffer = (byte)0;
                    } 
                    //Switch buffer
                    //Display image
                    //Fill other buffer async
                }

                OverallFrameCount++;
            }
        }

        public void Play()
        {
            if (/*AudioOpened && */ VideoOpened)
            {
                VideoTimer.Start();
                State = PlayerState.Playing;
                AudioPlayer.Play();
            }
        }

        public void Pause()
        {
            VideoTimer.Stop();
            State = PlayerState.Paused;
        }

        public void Stop()
        {
            VideoTimer.Stop();
            State = PlayerState.Stopped;
        }

        public bool OpenFile(String videoFileName)
        {
            if (File.Exists(videoFileName))
            {
                VideoPath = videoFileName;

                try
                {
                    FileReader = new BinaryReader(File.Open(VideoPath, FileMode.Open));
                    if (FileReader != null)
                    {
                        VideoOpened = true;
                        LoadBuffer(fileImageBufferOne);
                        Task.Factory.StartNew(() => { LoadBuffer(fileImageBufferTwo); });
                    }
                }
                catch
                {
                    FileReader.Dispose();
                    return false;
                }


                return true;
            }

            return false;
        }

        public bool OpenFile(String videoFileName, String audioFileName)
        {
            if (File.Exists(videoFileName) && File.Exists(audioFileName))
            {
                VideoPath = videoFileName;

                try
                {
                    FileReader = new BinaryReader(File.Open(VideoPath, FileMode.Open));
                    if (FileReader != null)
                    {
                        VideoOpened = true;
                        LoadBuffer(fileImageBufferOne);
                        Task.Factory.StartNew(() => { LoadBuffer(fileImageBufferTwo); });
                    }

                    AudioPlayer.SoundLocation = audioFileName;
                    AudioPlayer.LoadAsync();
                    
                }
                catch
                {
                    FileReader.Dispose();
                    return false;
                }
                    

                return true;
            }

            return false;
        }

        private void ConvertToRGBSequence(byte[] bufferToFill, byte[] bufferSource)
        {
            uint size = VideoHeight * VideoWidth;
            uint stride = size * 3;
            
            for (uint FrameIndex = 0; FrameIndex < (FrameRate * SecondsToBuffer); ++FrameIndex)
            {
                for (uint PixelIndex = 0; PixelIndex < size; PixelIndex++)
                {
                    bufferToFill[3 * PixelIndex + (uint)Color.RED + (stride * FrameIndex)] = bufferSource[PixelIndex + ((uint)Color.RED * size) + (stride * FrameIndex)];
                    bufferToFill[3 * PixelIndex + (uint)Color.GREEN + (stride * FrameIndex)] = bufferSource[PixelIndex + ((uint)Color.GREEN * size) + (stride * FrameIndex)];
                    bufferToFill[3 * PixelIndex + (uint)Color.BLUE + (stride * FrameIndex)] = bufferSource[PixelIndex + ((uint)Color.BLUE * size) + (stride * FrameIndex)];
                }
            }

        }

        private void GenerateFrame(byte [] sourceBuffer)
        {
            byte[] frameBuffer = new byte[VideoHeight * VideoWidth * 3];
            uint bufferFrame = OverallFrameCount % (SecondsToBuffer * FrameRate);
            int stride = (((int)VideoWidth * 24 + 31) & ~31) / 8;

            for (uint sourceStart = bufferFrame * VideoHeight * VideoWidth * 3, imageBufferStart = 0;
                imageBufferStart < VideoHeight * VideoWidth * 3;
                ++sourceStart,++imageBufferStart)
            {
                frameBuffer[imageBufferStart] = sourceBuffer[sourceStart];
            }

            Frame = BitmapSource.Create((int)VideoWidth,
                                        (int)VideoHeight,
                                        96,
                                        96,
                                        System.Windows.Media.PixelFormats.Rgb24,
                                        null,
                                        frameBuffer,
                                        stride);

        }

        private void LoadBuffer( byte[] buffer )
        {
            
            if (VideoOpened)
            {
                byte[] frameBuffer = new byte[VideoHeight * VideoWidth * 3 * FrameRate * SecondsToBuffer];
                    
                FileReader.Read(frameBuffer, 0, frameBuffer.Length);
                    
                ConvertToRGBSequence(buffer, frameBuffer);
            }           
        }
    }
}
