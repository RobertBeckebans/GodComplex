#include "Device.h"
#include "Components/Component.h"
#include "Components/Texture2D.h"
#include "Components/States.h"

Device::Device()
	: m_pDevice( NULL )
	, m_pDeviceContext( NULL )
	, m_pComponentsStackTop( NULL )
	, m_pCurrentMaterial( NULL )
	, m_pCurrentRasterizerState( NULL )
	, m_pCurrentDepthStencilState( NULL )
	, m_pCurrentBlendState( NULL )
{
}

int		Device::ComponentsCount() const
{
	int			Count = -2 - m_StatesCount;	// Start without counting for our internal back buffer & depth stencil components
	Component*	pCurrent = m_pComponentsStackTop;
	while ( pCurrent != NULL )
	{
		Count++;
		pCurrent = pCurrent->m_pPrevious;
	}

	return Count;
}

void	Device::Init( int _Width, int _Height, HWND _Handle, bool _Fullscreen, bool _sRGB )
{
	// Create a swap chain with 1 back buffer
	DXGI_SWAP_CHAIN_DESC	SwapChainDesc;

	// Simple output buffer
	SwapChainDesc.BufferDesc.Width = _Width;
	SwapChainDesc.BufferDesc.Height = _Height;
	SwapChainDesc.BufferDesc.Format = _sRGB ? DXGI_FORMAT_R8G8B8A8_UNORM_SRGB : DXGI_FORMAT_R8G8B8A8_UNORM;
//	SwapChainDesc.BufferDesc.Scaling = DXGI_MODE_SCALING_STRETCHED;
	SwapChainDesc.BufferDesc.Scaling = DXGI_MODE_SCALING_CENTERED;
	SwapChainDesc.BufferDesc.RefreshRate.Numerator = 60;
	SwapChainDesc.BufferDesc.RefreshRate.Denominator = 1;
	SwapChainDesc.BufferDesc.ScanlineOrdering = DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED;
	SwapChainDesc.BufferUsage = DXGI_USAGE_BACK_BUFFER | DXGI_USAGE_RENDER_TARGET_OUTPUT;
//	SwapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT | DXGI_USAGE_UNORDERED_ACCESS;
	SwapChainDesc.BufferCount = 1;

	// No multisampling
	SwapChainDesc.SampleDesc.Count = 1;
	SwapChainDesc.SampleDesc.Quality = 0;

	SwapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;
	SwapChainDesc.OutputWindow = _Handle;
	SwapChainDesc.Windowed = !_Fullscreen;
	SwapChainDesc.Flags = 0;

#ifdef DIRECTX11
	D3D_FEATURE_LEVEL	pFeatureLevels[] = { D3D_FEATURE_LEVEL_11_0 };	// Support D3D11 only...
#else
	D3D_FEATURE_LEVEL	pFeatureLevels[] = { D3D_FEATURE_LEVEL_10_0 };	// Support D3D10 only...
#endif
	D3D_FEATURE_LEVEL	ObtainedFeatureLevel;

 	Check
	(
		D3D11CreateDeviceAndSwapChain( NULL, D3D_DRIVER_TYPE_HARDWARE, NULL,
#ifdef _DEBUG
			D3D11_CREATE_DEVICE_DEBUG,
#else
			0,
#endif
			pFeatureLevels, 1,
			D3D11_SDK_VERSION,
			&SwapChainDesc, &m_pSwapChain,
			&m_pDevice, &ObtainedFeatureLevel, &m_pDeviceContext )
	);

	// Store the default render target
	ID3D11Texture2D*	pDefaultRenderSurface;
	m_pSwapChain->GetBuffer( 0, __uuidof( ID3D11Texture2D ), (void**) &pDefaultRenderSurface );
	ASSERT( pDefaultRenderSurface != NULL, "Failed to retrieve default render surface !" );
	m_pDefaultRenderTarget = new Texture2D( *this, *pDefaultRenderSurface, PixelFormatRGBA8::DESCRIPTOR );

	// Create the default depth stencil buffer
	m_pDefaultDepthStencil = new Texture2D( *this, _Width, _Height, DepthStencilFormatD32F::DESCRIPTOR );


	//////////////////////////////////////////////////////////////////////////
	// Create default render states
	m_StatesCount = 0;
	{
		D3D11_RASTERIZER_DESC	Desc;
		ASM_memset( &Desc, 0, sizeof(Desc) );
		Desc.FillMode = D3D11_FILL_SOLID;
        Desc.CullMode = D3D11_CULL_NONE;
        Desc.FrontCounterClockwise = TRUE;
        Desc.DepthBias = D3D11_DEFAULT_DEPTH_BIAS;
        Desc.DepthBiasClamp = D3D11_DEFAULT_DEPTH_BIAS_CLAMP;
        Desc.SlopeScaledDepthBias = D3D11_DEFAULT_SLOPE_SCALED_DEPTH_BIAS;
        Desc.DepthClipEnable = TRUE;
        Desc.ScissorEnable = FALSE;
        Desc.MultisampleEnable = FALSE;
        Desc.AntialiasedLineEnable = FALSE;

		m_pRS_CullNone = new RasterizerState( *this, Desc ); m_StatesCount++;
	}
	{
		D3D11_DEPTH_STENCIL_DESC	Desc;
		ASM_memset( &Desc, 0, sizeof(Desc) );
		Desc.DepthEnable = false;
		Desc.DepthWriteMask = D3D11_DEPTH_WRITE_MASK_ALL;
		Desc.DepthFunc = D3D11_COMPARISON_LESS;
		Desc.StencilEnable = false;
		Desc.StencilReadMask = 0;
		Desc.StencilWriteMask = 0;

		m_pDS_Disabled = new DepthStencilState( *this, Desc ); m_StatesCount++;

		Desc.DepthEnable = true;
		m_pDS_ReadWriteLess = new DepthStencilState( *this, Desc ); m_StatesCount++;
	}
	{
		D3D11_BLEND_DESC	Desc;
		ASM_memset( &Desc, 0, sizeof(Desc) );
		Desc.AlphaToCoverageEnable = false;
		Desc.IndependentBlendEnable = false;
		Desc.RenderTarget[0].BlendEnable = false;
		Desc.RenderTarget[0].SrcBlend = D3D11_BLEND_SRC_COLOR;
		Desc.RenderTarget[0].DestBlend = D3D11_BLEND_DEST_COLOR;
		Desc.RenderTarget[0].BlendOp = D3D11_BLEND_OP_ADD;
		Desc.RenderTarget[0].SrcBlendAlpha = D3D11_BLEND_SRC_ALPHA;
		Desc.RenderTarget[0].DestBlendAlpha = D3D11_BLEND_DEST_ALPHA;
		Desc.RenderTarget[0].BlendOpAlpha = D3D11_BLEND_OP_ADD;
		Desc.RenderTarget[0].RenderTargetWriteMask = 0x0F;		// Seems to crash on my card when setting more than 4 bits of write mask ! (limited to 4 MRTs I suppose ?)

		m_pBS_Disabled = new BlendState( *this, Desc ); m_StatesCount++;
	}

	//////////////////////////////////////////////////////////////////////////
	// Create default samplers
	D3D11_SAMPLER_DESC	Desc;
	Desc.Filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
	Desc.AddressU = Desc.AddressV = Desc.AddressW = D3D11_TEXTURE_ADDRESS_CLAMP;
	Desc.MipLODBias = 0.0f;
	Desc.MaxAnisotropy = 16;
	Desc.ComparisonFunc = D3D11_COMPARISON_NEVER;
	Desc.BorderColor[0] = Desc.BorderColor[2] = 1.0f;	Desc.BorderColor[1] = Desc.BorderColor[3] = 0.0f;
	Desc.MinLOD = -D3D11_FLOAT32_MAX;
	Desc.MaxLOD = D3D11_FLOAT32_MAX;

	m_pDevice->CreateSamplerState( &Desc, &m_ppSamplers[0] );	// Linear Clamp
	Desc.Filter = D3D11_FILTER_MIN_MAG_MIP_POINT;
	m_pDevice->CreateSamplerState( &Desc, &m_ppSamplers[1] );	// Point Clamp

	Desc.AddressU = Desc.AddressV = Desc.AddressW = D3D11_TEXTURE_ADDRESS_WRAP;
	m_pDevice->CreateSamplerState( &Desc, &m_ppSamplers[3] );	// Point Wrap
	Desc.Filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
	m_pDevice->CreateSamplerState( &Desc, &m_ppSamplers[2] );	// Linear Wrap

	Desc.AddressU = Desc.AddressV = Desc.AddressW = D3D11_TEXTURE_ADDRESS_MIRROR;
	m_pDevice->CreateSamplerState( &Desc, &m_ppSamplers[4] );	// Linear Mirror
	Desc.Filter = D3D11_FILTER_MIN_MAG_MIP_POINT;
	m_pDevice->CreateSamplerState( &Desc, &m_ppSamplers[5] );	// Point Mirror

	// Upload them once and for all
	m_pDeviceContext->VSSetSamplers( 0, SAMPLERS_COUNT, m_ppSamplers );
	m_pDeviceContext->GSSetSamplers( 0, SAMPLERS_COUNT, m_ppSamplers );
	m_pDeviceContext->PSSetSamplers( 0, SAMPLERS_COUNT, m_ppSamplers );
}

void	Device::Exit()
{
	if ( m_pDevice == NULL )
		return; // Already released !

	// Dispose of all the registered components in reverse order
	while ( m_pComponentsStackTop != NULL )
		delete m_pComponentsStackTop;  // DIE !!

	// Dispose of samplers
	for ( int SamplerIndex=0; SamplerIndex < SAMPLERS_COUNT; SamplerIndex++ )
		m_ppSamplers[SamplerIndex]->Release();

	m_pSwapChain->Release();

	m_pDeviceContext->ClearState();
	m_pDeviceContext->Flush();

	m_pDeviceContext->Release(); m_pDeviceContext = NULL;
	m_pDevice->Release(); m_pDevice = NULL;
}

void	Device::ClearRenderTarget( const Texture2D& _Target, const NjFloat4& _Color )
{
	m_pDeviceContext->ClearRenderTargetView( _Target.GetTargetView( 0, 0, 0 ), &_Color.x );
}

void	Device::ClearDepthStencil( const Texture2D& _DepthStencil, float _Z, U8 _Stencil )
{
	m_pDeviceContext->ClearDepthStencilView( _DepthStencil.GetDepthStencilView(), D3D11_CLEAR_DEPTH | D3D11_CLEAR_STENCIL, _Z, _Stencil );
}

void	Device::SetRenderTarget( const Texture2D& _Target, const Texture2D* _pDepthStencil, D3D11_VIEWPORT* _pViewport )
{
	ID3D11RenderTargetView*	pTargetView = _Target.GetTargetView( 0, 0, 0 );
	ID3D11DepthStencilView*	pDepthStencilView = _pDepthStencil != NULL ? _pDepthStencil->GetDepthStencilView() : NULL;

	if ( _pViewport == NULL )
	{	// Use default viewport
		D3D11_VIEWPORT	Viewport;
		Viewport.TopLeftX = 0;
		Viewport.TopLeftY = 0;
		Viewport.Width = float(_Target.GetWidth());
		Viewport.Height = float(_Target.GetHeight());
		Viewport.MinDepth = 0.0f;
		Viewport.MaxDepth = 1.0f;
		m_pDeviceContext->RSSetViewports( 1, &Viewport );
	}
	else
		m_pDeviceContext->RSSetViewports( 1, _pViewport );

	m_pDeviceContext->OMSetRenderTargets( 1, &pTargetView, pDepthStencilView );
}

void	Device::SetStates( RasterizerState& _RasterizerState, DepthStencilState& _DepthStencilState, BlendState& _BlendState )
{
	if ( &_RasterizerState != m_pCurrentRasterizerState )
		m_pDeviceContext->RSSetState( _RasterizerState.m_pState );
	m_pCurrentRasterizerState = &_RasterizerState;

	if ( &_DepthStencilState != m_pCurrentDepthStencilState )
		m_pDeviceContext->OMSetDepthStencilState( _DepthStencilState.m_pState, 0 );
	m_pCurrentDepthStencilState = &_DepthStencilState;

	if ( &_BlendState != m_pCurrentBlendState )
		m_pDeviceContext->OMSetBlendState( _BlendState.m_pState, &NjFloat4::One.x, ~0L );
	m_pCurrentBlendState = &_BlendState;
}

void	Device::RegisterComponent( Component& _Component )
{
	// Attach to the end of the list
	if ( m_pComponentsStackTop != NULL )
		m_pComponentsStackTop->m_pNext = &_Component;
	_Component.m_pPrevious = m_pComponentsStackTop;

	m_pComponentsStackTop = &_Component;
}

void	Device::UnRegisterComponent( Component& _Component )
{
	// Link over
	if ( _Component.m_pPrevious != NULL )
		_Component.m_pPrevious->m_pNext = _Component.m_pNext;
	if ( _Component.m_pNext != NULL )
		_Component.m_pNext->m_pPrevious = _Component.m_pPrevious;
	else
		m_pComponentsStackTop = _Component.m_pPrevious;	// We were the top of the stack !
}

void	Device::Check( HRESULT _Result )
{
#ifdef _DEBUG
	if ( _Result != S_OK )
		PostQuitMessage( _Result );
	ASSERT( _Result == S_OK, "DX HRESULT Check failed !" );
#endif
}
 
