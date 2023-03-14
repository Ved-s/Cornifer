using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer
{
    public class TaskProgress : IDisposable
    {
        internal static UIAutoSizePanel? StatusPanel;
        internal static UIList? StatusPanelList;
        static List<TaskProgress> Tasks = new();

        public string Title
        {
            get => TitleLabel.Text ?? "";
            set
            {
                TitleLabel.Text = value;
                StatusPanel?.Recalculate();
            }
        }
        public float? MaxProgress 
        {
            get => ProgressBar?.MaxProgress;
            set 
            {
                if (value is null && ProgressBar is not null)
                {
                    PanelList.Elements.Remove(ProgressBar);
                    ProgressBar = null;
                    StatusPanel?.Recalculate();
                }
                else if (value is not null)
                {
                    if (ProgressBar is null)
                    {
                        ProgressBar = new()
                        {
                            Height = 12,
                        };
                        PanelList.Elements.Add(ProgressBar);
                        StatusPanel?.Recalculate();
                    }
                    ProgressBar.MaxProgress = value.Value;
                }
            }
        }
        public float Progress 
        {
            get => ProgressBar?.Progress ?? 0;
            set 
            {
                if (ProgressBar is null)
                    return;

                ProgressBar.Progress = value;
            }
        }

        public UIAutoSizePanel Panel { get; }

        UIList PanelList;
        UILabel TitleLabel;
        UIProgressBar? ProgressBar;

        public TaskProgress()
        {
            Panel = new()
            {
                Padding = 4,
                Height = 0,
                AutoWidth = false,
                BorderColor = Color.Transparent,
                BackColor = Color.Transparent,

                Elements =
                {
                    new UIList
                    {
                        Height = 0,
                        AutoSize = true,
                        ElementSpacing = 4,

                        Elements =
                        {
                            new UILabel
                            {
                                Height = 0,
                                TextAlign = new(.5f)
                            }.Assign(out TitleLabel)
                        }
                    }.Assign(out PanelList)
                }
            };
        }

        public TaskProgress(string title, float? maxProgress) : this()
        {
            Title = title;
            MaxProgress = maxProgress;
            Start();
        }

        void Start()
        {
            Tasks.Add(this);

            if (StatusPanelList is not null)
                StatusPanelList.Elements.Add(Panel);

            if (StatusPanel is not null)
            {
                StatusPanel.Visible = Tasks.Count > 0;
                if (StatusPanel.Visible)
                    StatusPanel.Recalculate();
            }
        }

        void Stop()
        {
            Tasks.Remove(this);

            if (StatusPanelList is not null)
                StatusPanelList.Elements.Remove(Panel);

            if (StatusPanel is not null)
            {
                StatusPanel.Visible = Tasks.Count > 0;
                if (StatusPanel.Visible)
                    StatusPanel.Recalculate();
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public static UIAutoSizePanel InitUI()
        {
            StatusPanel = new()
            {
                Width = 250,
                Top = 5,
                Left = new(0, .5f, -.5f),
                AutoWidth = false,
                Height = 0,

                Padding = 4,
                Visible = Tasks.Count > 0,

                Elements = 
                {
                    new UIList
                    { 
                        AutoSize = true,
                    }.Assign(out StatusPanelList)
                }
            };

            foreach (TaskProgress progress in Tasks)
                StatusPanelList.Elements.Add(progress.Panel);

            return StatusPanel;
        }
    }
}
