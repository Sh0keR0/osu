﻿using OpenTK;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API;

namespace osu.Game.Overlays.Options.General
{
    public class LoginOptions : OptionsSubsection
    {
        private Container loginForm;
        protected override string Header => "Sign In";

        public LoginOptions()
        {
            Children = new[]
            {
                loginForm = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Children = new[] { new LoadingAnimation() }
                }
            };
        }

        [BackgroundDependencyLoader(permitNulls: true)]
        private void load(APIAccess api)
        {
            if (api == null)
                return;
            loginForm.Children = new Drawable[]
            {
                new LoginForm(api)
            };
        }

        class LoginForm : FlowContainer
        {
            public LoginForm(APIAccess api)
            {
                Direction = FlowDirection.VerticalOnly;
                AutoSizeAxes = Axes.Y;
                RelativeSizeAxes = Axes.X;
                Spacing = new Vector2(0, 5);
                // TODO: Wire things up
                Children = new Drawable[]
                {
                    new SpriteText { Text = "Username" },
                    new TextBox { Height = 20, RelativeSizeAxes = Axes.X, Text = api?.Username ?? string.Empty },
                    new SpriteText { Text = "Password" },
                    new TextBox { Height = 20, RelativeSizeAxes = Axes.X },
                    new OsuButton
                    {
                        RelativeSizeAxes = Axes.X,
                        Text = "Log in",
                    }
                };
            }
        }
    }
}
