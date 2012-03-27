﻿// Copyright (c) 2010-2011 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D;
using Windows.UI.Core;

namespace CommonDX
{
    /// <summary>
    /// This class is a straight conversion of the C++ DirectXBase class found 
    /// from Windows 8 Metro-Style App Samples.
    /// </summary>
    /// <remarks>
    /// The difference between the original samples is that the swap chain
    /// description can be overrided in a separate method, as well as the
    /// creation of the swap chain.
    /// </remarks>
    public class DeviceManager : Component
    {
        // Declare Direct2D Objects
        protected SharpDX.Direct2D1.Factory1           d2dFactory;
        protected SharpDX.Direct2D1.Device             d2dDevice;
        protected SharpDX.Direct2D1.DeviceContext      d2dContext;

        // Declare DirectWrite & Windows Imaging Component Objects
        protected SharpDX.DirectWrite.Factory          dwriteFactory;
        protected SharpDX.WIC.ImagingFactory2          wicFactory;

        // Direct3D Objects
        protected SharpDX.Direct3D11.Device1           d3dDevice;
        protected SharpDX.Direct3D11.DeviceContext1    d3dContext;
        protected FeatureLevel featureLevel;
        protected float dpi;

        /// <summary>
        /// Gets the Direct3D11 device.
        /// </summary>
        public SharpDX.Direct3D11.Device1 DeviceDirect3D { get { return d3dDevice; } }

        /// <summary>
        /// Gets the Direct3D11 context.
        /// </summary>
        public SharpDX.Direct3D11.DeviceContext1 ContextDirect3D { get { return d3dContext; } }

        /// <summary>
        /// Gets the Direct2D factory.
        /// </summary>
        public SharpDX.Direct2D1.Factory1 FactoryDirect2D { get { return d2dFactory; } }

        /// <summary>
        /// Gets the Direct2D device.
        /// </summary>
        public SharpDX.Direct2D1.Device DeviceDirect2D { get { return d2dDevice; } }

        /// <summary>
        /// Gets the Direct2D context.
        /// </summary>
        public SharpDX.Direct2D1.DeviceContext ContextDirect2D { get { return d2dContext; } }

        /// <summary>
        /// Gets the DirectWrite factory.
        /// </summary>
        public SharpDX.DirectWrite.Factory FactoryDirectWrite { get { return dwriteFactory; } }

        /// <summary>
        /// Gets the WIC factory.
        /// </summary>
        public SharpDX.WIC.ImagingFactory2 WICFactory { get { return wicFactory; } }

        /// <summary>
        /// This event is fired when the <see cref="Dpi"/> is called,
        /// </summary>
        public event Action<DeviceManager> OnDpiChanged;

        /// <summary>
        /// This event is fired when the DeviceMamanger is initialized by the <see cref="Initialize"/> method.
        /// </summary>
        public event Action<DeviceManager> OnInitialize;

        /// <summary>
        /// Initialize this instance.
        /// </summary>
        /// <param name="window">Window to receive the rendering</param>
        public virtual void Initialize(float dpi)
        {
            CreateDeviceIndependentResources();
            CreateDeviceResources();
            
            if (OnInitialize != null)
                OnInitialize(this);

            Dpi = dpi;
        }

        /// <summary>
        /// Creates device independent resources.
        /// </summary>
        /// <remarks>
        /// This method is called at the initialization of this instance.
        /// </remarks>
        protected virtual void CreateDeviceIndependentResources() {
#if DEBUG
            var debugLevel = SharpDX.Direct2D1.DebugLevel.Information;
#else
            var debugLevel = SharpDX.Direct2D1.DebugLevel.None;
#endif
            // Dispose previous references and set to null
            SafeDispose(ref d2dFactory);
            SafeDispose(ref dwriteFactory);
            SafeDispose(ref wicFactory);

            // Allocate new references
            d2dFactory = ToDispose(new SharpDX.Direct2D1.Factory1(SharpDX.Direct2D1.FactoryType.SingleThreaded, debugLevel));
            dwriteFactory = ToDispose(new SharpDX.DirectWrite.Factory(SharpDX.DirectWrite.FactoryType.Shared));
            wicFactory = ToDispose(new SharpDX.WIC.ImagingFactory2());
        }

        /// <summary>
        /// Creates device resources. 
        /// </summary>
        /// <remarks>
        /// This method is called at the initialization of this instance.
        /// </remarks>
        protected virtual void CreateDeviceResources()
        {
            // Dispose previous references and set to null
            SafeDispose(ref d3dDevice);
            SafeDispose(ref d3dContext);
            SafeDispose(ref d2dDevice);
            SafeDispose(ref d2dContext);

            // Allocate new references
            // Enable compatibility with Direct2D
            // Retrieve the Direct3D 11.1 device amd device context
            var creationFlags = SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport;
#if DEBUG
            // creationFlags |= SharpDX.Direct3D11.DeviceCreationFlags.Debug;
#endif
            using (var defaultDevice = new SharpDX.Direct3D11.Device(DriverType.Hardware, creationFlags))
                d3dDevice = defaultDevice.QueryInterface<SharpDX.Direct3D11.Device1>();
            featureLevel = d3dDevice.FeatureLevel;

            // Get Direct3D 11.1 context
            d3dContext = ToDispose(d3dDevice.ImmediateContext.QueryInterface<SharpDX.Direct3D11.DeviceContext1>());

            // Create Direct2D device
            using (var dxgiDevice = d3dDevice.QueryInterface<SharpDX.DXGI.Device>())
                d2dDevice = ToDispose(new SharpDX.Direct2D1.Device(dxgiDevice));

            // Create Direct2D context
            d2dContext = ToDispose(new SharpDX.Direct2D1.DeviceContext(d2dDevice, SharpDX.Direct2D1.DeviceContextOptions.None));
        }

        /// <summary>
        /// Gets or sets the DPI.
        /// </summary>
        public virtual float Dpi
        {
            get
            {
                return dpi;
            }
            set
            {
                if (dpi != value)
                {
                    dpi = value;
                    d2dContext.DotsPerInch = new SharpDX.DrawingSizeF(dpi, dpi);

                    if (OnDpiChanged != null)
                        OnDpiChanged(this);
                }
            }
        }
    }
}
