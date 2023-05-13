using Cornifer.Input;
using Cornifer.MapObjects;
using Cornifer.Structures;
using Cornifer.UI;
using Cornifer.UI.Elements;
using Cornifer.UI.Modals;
using Cornifer.UI.Pages;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Cornifer
{
    public static class Interface
    {
        public static UIRoot? Root;

        public static UIPanel SidePanel = null!;
        public static ColorSelector ColorSelector = null!;

        public static bool Hovered => Root?.Hover is not null;
        public static bool Active => Root?.Active is not null;
        public static bool BlockUIHover => Main.Selecting || Main.Dragging || InputHandler.Pan.Pressed && !Hovered;

        delegate UIModal CreateModalDelegate(bool cached);

        static List<CreateModalDelegate>? ModalCreators;
        static Queue<TaskCompletionSource> ModalWaitTasks = new();
        internal static UIModal? CurrentModal;
        internal static TabContainer? Tabs;

        public static void Init()
        {
            Config.Init();

            Root = new(Main.Instance)
            {
                Font = Content.Consolas10,

                Elements =
                {
                    TaskProgress.InitUI(),

                    new UIResizeablePanel()
                    {
                        Left = new(0, 1, -1),

                        Width = 200,
                        Height = new(0, 1),
                        MaxWidth = new(0, 1f),
                        MinWidth = new(80, 0),

                        Margin = 5,

                        BackColor = Color.Transparent,
                        BorderColor = Color.Transparent,

                        CanGrabTop = false,
                        CanGrabRight = false,
                        CanGrabBottom = false,
                        SizingChangesPosition = false,

                        Elements =
                        {
                            new TabContainer()
                                .Execute(Page.CreatePages)
                                .Assign(out Tabs)
                        }
                    }.Assign(out SidePanel),

                    new ColorSelector
                    {
                        Top = 5,
                        Left = 5,
                        Visible = false,
                    }.Assign(out ColorSelector)
                }
            };
            CreateModals();
            Root.Recalculate();

            if (Main.Region is not null)
                RegionChanged(Main.Region);
        }

        static void CreateModals()
        {
            bool useCachedModals = false;
            if (ModalCreators is null)
            {
                useCachedModals = true; // If modals were created before init
                Type modalType = typeof(Modal<,>);
                ModalCreators = new();
                Type[] args = new[] { typeof(bool) };
                foreach (Type type in Assembly.GetExecutingAssembly().GetExportedTypes())
                {
                    if (type.BaseType is null || !type.BaseType.IsGenericType || type.BaseType.GetGenericTypeDefinition() != modalType)
                        continue;

                    MethodInfo? creatorMethod = type.BaseType.GetMethod("CreateUIElement", BindingFlags.Public | BindingFlags.Static, args);
                    if (creatorMethod is null || creatorMethod.ReturnType != typeof(UIModal))
                        continue;

                    CreateModalDelegate creator = creatorMethod.CreateDelegate<CreateModalDelegate>();
                    ModalCreators.Add(creator);
                }
            }
            if (Root is not null)
                foreach (CreateModalDelegate creator in ModalCreators)
                    Root.Elements.Add(creator(useCachedModals));
        }
        internal static void ModalClosed()
        {
            CurrentModal = null;
            while (ModalWaitTasks.TryDequeue(out TaskCompletionSource? waitingTask))
            {
                waitingTask.SetResult();
                if (CurrentModal is not null)
                    return;
            }
        }

        public static async Task WaitModal()
        {
            if (CurrentModal is null)
                return;

            TaskCompletionSource waitingTask = new();
            ModalWaitTasks.Enqueue(waitingTask);

            await waitingTask.Task;
        }

        
        public static void RegionChanged(Region region)
        {
            Subregions.RegionChanged(region);
            Visibility.RegionChanged();
        }

        public static void Update()
        {
            Root?.Update();

            if (InputHandler.ReinitUI.JustPressed)
            {
                string? selectedTabName = Tabs?.Tabs.FirstOrDefault(t => t.Selected)?.Name;
                Init();

                if (selectedTabName is not null)
                {
                    TabContainer.Tab? tab = Tabs?.Tabs.FirstOrDefault(t => t.Name == selectedTabName);
                    if (tab is not null)
                    {
                        Tabs!.SelectedTab = tab;
                    }
                }
            }
        }
        public static void Draw()
        {
            Root?.Draw(Main.SpriteBatch);
        }
    }
}
