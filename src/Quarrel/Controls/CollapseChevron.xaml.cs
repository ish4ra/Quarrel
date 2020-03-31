﻿// Copyright (c) Quarrel. All rights reserved.

using Microsoft.Toolkit.Uwp.UI.Animations;
using Windows.UI.Xaml.Controls;

namespace Quarrel.Controls
{
    /// <summary>
    /// Control for a chevron to indicate collapsed status of Category in ChannelTemplate.
    /// </summary>
    public sealed partial class CollapseChveron : UserControl
    {
        private bool _isCollapsed;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseChveron"/> class.
        /// </summary>
        public CollapseChveron()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the category is collapsed.
        /// </summary>
        /// <remarks> Rotates FontIcon -90 when collapsed.</remarks>
        public bool IsCollapsed
        {
            get => _isCollapsed;
            set
            {
                if (value)
                {
                    Chevron.Rotate(-90, 7, 7, 400, 0, EasingType.Circle).Start();
                }
                else
                {
                    Chevron.Rotate(0, 7, 7, 400, 0, EasingType.Circle).Start();
                }

                _isCollapsed = value;
            }
        }
    }
}
