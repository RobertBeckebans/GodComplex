
#include "Material.h"
#include "ConstantBuffer.h"

#include <stdio.h>
#include <io.h>

#include "D3Dcompiler.h"
#include "D3D11Shader.h"


Material::Material( Device& _Device, const IVertexFormatDescriptor& _Format, const char* _pShaderFileName, const char* _pShaderCode, D3D_SHADER_MACRO* _pMacros, const char* _pEntryPointVS, const char* _pEntryPointGS, const char* _pEntryPointPS, ID3DInclude* _pIncludeOverride )
	: Component( _Device )
	, m_Format( _Format )
	, m_pVertexLayout( NULL )
	, m_pVS( NULL )
	, m_pGS( NULL )
	, m_pPS( NULL )
	, m_pShaderPath( NULL )
{
	m_pIncludeOverride = _pIncludeOverride;
	m_bHasErrors = false;

	// Store the default NULL pointer to point to the shader path
	m_pShaderFileName = _pShaderFileName;

#ifndef GODCOMPLEX
	m_pShaderPath = GetShaderPath( _pShaderFileName );
	m_Pointer2FileName.Add( NULL, m_pShaderPath );
#endif

#ifdef _DEBUG
	if ( _pShaderFileName != NULL )
	{
		// Just ensure the file exists !
		FILE*	pFile;
		fopen_s( &pFile, _pShaderFileName, "rb" );
		ASSERT( pFile != NULL, "Shader file not found !" );
		fclose( pFile );

		// Register as a watched shader
		ms_WatchedShaders.Add( _pShaderFileName, this );

		m_LastShaderModificationTime = GetFileModTime( _pShaderFileName );
	}
#endif

	m_pEntryPointVS = _pEntryPointVS;
	m_pEntryPointGS = _pEntryPointGS;
	m_pEntryPointPS = _pEntryPointPS;

	if ( _pMacros != NULL )
	{
		D3D_SHADER_MACRO*	pMacro = _pMacros;
		while ( pMacro->Name != NULL )
			pMacro++;

		int	MacrosCount = 1 + pMacro - _pMacros;
		m_pMacros = new D3D_SHADER_MACRO[MacrosCount];
		memcpy( m_pMacros, _pMacros, MacrosCount*sizeof(D3D_SHADER_MACRO) );
	}
	else
		m_pMacros = NULL;

	CompileShaders( _pShaderCode );
}

Material::~Material()
{
#ifdef _DEBUG
	// Unregister as a watched shader
	if ( m_pShaderFileName != NULL )
		ms_WatchedShaders.Remove( m_pShaderFileName );
#endif

	if ( m_pShaderPath != NULL ) delete[] m_pShaderPath;
	if ( m_pVertexLayout != NULL ) { m_pVertexLayout->Release(); m_pVertexLayout = NULL; }
	if ( m_pVS != NULL ) { m_pVS->Release(); m_pVS = NULL; }
	if ( m_pGS != NULL ) { m_pGS->Release(); m_pGS = NULL; }
	if ( m_pPS != NULL ) { m_pPS->Release(); m_pPS = NULL; }

	if ( m_pMacros != NULL ) delete[] m_pMacros;
}

void	Material::CompileShaders( const char* _pShaderCode )
{
	// Release any pre-existing shader
	if ( m_pVertexLayout != NULL ) m_pVertexLayout->Release();
	if ( m_pVS != NULL )	m_pVS->Release();
	if ( m_pGS != NULL )	m_pGS->Release();
	if ( m_pPS != NULL )	m_pPS->Release();

	// Compile the compulsory vertex shader
	ASSERT( m_pEntryPointVS != NULL, "Invalid VertexShader entry point !" );
	ID3DBlob*   pShader = CompileShader( _pShaderCode, m_pMacros, m_pEntryPointVS, "vs_4_0" );
	if ( pShader != NULL )
	{
		Check( m_Device.DXDevice().CreateVertexShader( pShader->GetBufferPointer(), pShader->GetBufferSize(), NULL, &m_pVS ) );
		ASSERT( m_pVS != NULL, "Failed to create vertex shader !" );
#ifndef GODCOMPLEX
		m_VSConstants.Enumerate( *pShader );
#endif
		m_bHasErrors |= m_pVS == NULL;

		// Create the associated vertex layout
		Check( m_Device.DXDevice().CreateInputLayout( m_Format.GetInputElements(), m_Format.GetInputElementsCount(), pShader->GetBufferPointer(), pShader->GetBufferSize(), &m_pVertexLayout ) );
		ASSERT( m_pVertexLayout != NULL, "Failed to create vertex layout !" );
		m_bHasErrors |= m_pVertexLayout == NULL;

		pShader->Release();
	}
	else
		m_bHasErrors = true;

	// Compile the optional geometry shader
	if ( !m_bHasErrors && m_pEntryPointGS != NULL )
	{
		ID3DBlob*   pShader = CompileShader( _pShaderCode, m_pMacros, m_pEntryPointGS, "gs_4_0" );
		if ( pShader != NULL )
		{
			Check( m_Device.DXDevice().CreateGeometryShader( pShader->GetBufferPointer(), pShader->GetBufferSize(), NULL, &m_pGS ) );
			ASSERT( m_pGS != NULL, "Failed to create geometry shader !" );
#ifndef GODCOMPLEX
			m_GSConstants.Enumerate( *pShader );
#endif
			m_bHasErrors |= m_pGS == NULL;

			pShader->Release();
		}
		else
			m_bHasErrors = true;
	}

	// Compile the optional pixel shader
	if ( !m_bHasErrors && m_pEntryPointPS != NULL )
	{
		ID3DBlob*   pShader = CompileShader( _pShaderCode, m_pMacros, m_pEntryPointPS, "ps_4_0" );
		if ( pShader != NULL )
		{
			Check( m_Device.DXDevice().CreatePixelShader( pShader->GetBufferPointer(), pShader->GetBufferSize(), NULL, &m_pPS ) );
			ASSERT( m_pPS != NULL, "Failed to create pixel shader !" );
#ifndef GODCOMPLEX
			m_PSConstants.Enumerate( *pShader );
#endif
			m_bHasErrors |= m_pPS == NULL;

			pShader->Release();
		}
		else
			m_bHasErrors = true;
	}
}

void	DeleteChars( const char*& _pValue, void* _pUserData )	{ delete[] _pValue; }

void	Material::Use()
{
	m_Device.DXContext().IASetInputLayout( m_pVertexLayout );
	m_Device.DXContext().VSSetShader( m_pVS, NULL, 0 );
	m_Device.DXContext().GSSetShader( m_pGS, NULL, 0 );
	m_Device.DXContext().PSSetShader( m_pPS, NULL, 0 );
}

// Embedded shader for debug & testing...
// static char*	pTestShader =
// 	"struct VS_IN\r\n" \
// 	"{\r\n" \
// 	"	float4	__Position : SV_POSITION;\r\n" \
// 	"};\r\n" \
// 	"\r\n" \
// 	"VS_IN	VS( VS_IN _In ) { return _In; }\r\n" \
// 	"\r\n" \
// 	"\r\n" \
// 	"\r\n" \
// 	"\r\n" \
// 	"\r\n" \
// 	"\r\n" \
// 	"\r\n" \
// 	"\r\n" \
// 	"\r\n" \
// 	"\r\n" \
// 	"\r\n" \
// 	"";

ID3DBlob*   Material::CompileShader( const char* _pShaderCode, D3D_SHADER_MACRO* _pMacros, const char* _pEntryPoint, const char* _pTarget )
{
	ID3DBlob*   pCodeText;
	ID3DBlob*   pCode;
	ID3DBlob*   pErrors;

//_pShaderCode = pTestShader;

	D3DPreprocess( _pShaderCode, strlen(_pShaderCode), NULL, _pMacros, this, &pCodeText, &pErrors );
#if defined(_DEBUG) || defined(DEBUG_SHADER)
	if ( pErrors != NULL )
	{
		MessageBoxA( NULL, (LPCSTR) pErrors->GetBufferPointer(), "Shader PreProcess Error !", MB_OK | MB_ICONERROR );
		ASSERT( pErrors == NULL, "Shader preprocess error !" );
	}
#endif

	U32 Flags1 = 0;
#ifdef _DEBUG
		Flags1 |= D3D10_SHADER_DEBUG;
		Flags1 |= D3D10_SHADER_SKIP_OPTIMIZATION;
//		Flags1 |= D3D10_SHADER_WARNINGS_ARE_ERRORS;
#else
		Flags1 |= D3D10_SHADER_OPTIMIZATION_LEVEL3;
#endif
		Flags1 |= D3D10_SHADER_ENABLE_STRICTNESS;
		Flags1 |= D3D10_SHADER_IEEE_STRICTNESS;
		Flags1 |= D3D10_SHADER_PACK_MATRIX_ROW_MAJOR;	// MOST IMPORTANT FLAG !

	U32 Flags2 = 0;

	LPCVOID	pCodePointer = pCodeText->GetBufferPointer();
	size_t	CodeSize = pCodeText->GetBufferSize();
	size_t	CodeLength = strlen( (char*) pCodePointer );

	D3DCompile( pCodePointer, CodeSize, NULL, _pMacros, this, _pEntryPoint, _pTarget, Flags1, Flags2, &pCode, &pErrors );
#if defined(_DEBUG) || defined(DEBUG_SHADER)
	if ( pCode == NULL && pErrors != NULL )
	{
		MessageBoxA( NULL, (LPCSTR) pErrors->GetBufferPointer(), "Shader Compilation Error !", MB_OK | MB_ICONERROR );
		ASSERT( pErrors == NULL, "Shader compilation error !" );
	}
	else
		ASSERT( pCode != NULL, "Shader compilation failed => No error provided but didn't output any shader either !" );
#endif

	return pCode;
}

HRESULT	Material::Open( THIS_ D3D_INCLUDE_TYPE _IncludeType, LPCSTR _pFileName, LPCVOID _pParentData, LPCVOID* _ppData, UINT* _pBytes )
{
	if ( m_pIncludeOverride != NULL )
		return m_pIncludeOverride->Open( _IncludeType, _pFileName, _pParentData, _ppData, _pBytes );

#ifndef GODCOMPLEX
	const char**	ppShaderPath = m_Pointer2FileName.Get( U32(_pParentData) );
	ASSERT( ppShaderPath != NULL, "Failed to retrieve data pointer !" );
	const char*	pShaderPath = *ppShaderPath;

	char	pFullName[4096];
	sprintf_s( pFullName, 4096, "%s%s", pShaderPath, _pFileName );

	FILE*	pFile;
	fopen_s( &pFile, pFullName, "rb" );
	ASSERT( pFile != NULL, "Include file not found !" );

	fseek( pFile, 0, SEEK_END );
	U32	Size = ftell( pFile );
	fseek( pFile, 0, SEEK_SET );

	char*	pBuffer = new char[Size];
	fread_s( pBuffer, Size, 1, Size, pFile );
//	pBuffer[Size] = '\0';

	*_pBytes = Size;
	*_ppData = pBuffer;

	fclose( pFile );

	// Register this shader's path as attached to the data pointer
	const char*	pIncludedShaderPath = GetShaderPath( pFullName );
	m_Pointer2FileName.Add( U32(*_ppData), pIncludedShaderPath );
#else
	ASSERT( false, "You MUST provide an ID3DINCLUDE override when compiling with the GODCOMPLEX option !" );
#endif

	return S_OK;
}

HRESULT	Material::Close( THIS_ LPCVOID _pData )
{
	if ( m_pIncludeOverride != NULL )
		return m_pIncludeOverride->Close( _pData );

#ifndef GODCOMPLEX
	// Remove entry from dictionary
	const char**	ppShaderPath = m_Pointer2FileName.Get( U32(_pData) );
	ASSERT( ppShaderPath != NULL, "Failed to retrieve data pointer !" );
	delete[] *ppShaderPath;
	m_Pointer2FileName.Remove( U32(_pData) );

	// Delete file content
	delete[] _pData;
#endif

	return S_OK;
}

#ifndef GODCOMPLEX
const char*	Material::GetShaderPath( const char* _pShaderFileName ) const
{
	char*	pResult = NULL;
	if ( _pShaderFileName != NULL )
	{
		int	FileNameLength = strlen(_pShaderFileName)+1;
		pResult = new char[FileNameLength];
		strcpy_s( pResult, FileNameLength, _pShaderFileName );

		char*	pLastSlash = strrchr( pResult, '\\' );
		if ( pLastSlash == NULL )
			pLastSlash = strrchr( pResult, '/' );
		if ( pLastSlash != NULL )
			pLastSlash[1] = '\0';
	}

	if ( pResult == NULL )
	{	// Empty string...
		pResult = new char[1];
		pResult = '\0';
		return pResult;
	}

	return pResult;
}
#endif

void	Material::SetConstantBuffer( int _BufferSlot, ConstantBuffer& _Buffer )
{
	ID3D11Buffer*	pBuffer = _Buffer.GetBuffer();
	m_Device.DXContext().VSSetConstantBuffers( _BufferSlot, 1, &pBuffer );
	m_Device.DXContext().GSSetConstantBuffers( _BufferSlot, 1, &pBuffer );
	m_Device.DXContext().PSSetConstantBuffers( _BufferSlot, 1, &pBuffer );
}

void	Material::SetTexture( int _BufferSlot, ID3D11ShaderResourceView* _pData )
{
	m_Device.DXContext().VSSetShaderResources( _BufferSlot, 1, &_pData );
	m_Device.DXContext().GSSetShaderResources( _BufferSlot, 1, &_pData );
	m_Device.DXContext().PSSetShaderResources( _BufferSlot, 1, &_pData );
}

#ifndef GODCOMPLEX
bool	Material::SetConstantBuffer( const char* _pBufferName, ConstantBuffer& _Buffer )
{
	bool	bUsed = false;
	ID3D11Buffer*	pBuffer = _Buffer.GetBuffer();

	{
		int	SlotIndex = m_VSConstants.GetConstantBufferIndex( _pBufferName );
		if ( SlotIndex != -1 )
			m_Device.DXContext().VSSetConstantBuffers( SlotIndex, 1, &pBuffer );
		bUsed |= SlotIndex != -1;
	}
	{
		int	SlotIndex = m_GSConstants.GetConstantBufferIndex( _pBufferName );
		if ( SlotIndex != -1 )
			m_Device.DXContext().GSSetConstantBuffers( SlotIndex, 1, &pBuffer );
		bUsed |= SlotIndex != -1;
	}
	{
		int	SlotIndex = m_PSConstants.GetConstantBufferIndex( _pBufferName );
		if ( SlotIndex != -1 )
			m_Device.DXContext().PSSetConstantBuffers( SlotIndex, 1, &pBuffer );
		bUsed |= SlotIndex != -1;
	}

	return	bUsed;
}

bool	Material::SetTexture( const char* _pBufferName, ID3D11ShaderResourceView* _pData )
{
	bool	bUsed = false;
	{
		int	SlotIndex = m_VSConstants.GetShaderResourceViewIndex( _pBufferName );
		if ( SlotIndex != -1 )
			m_Device.DXContext().VSSetShaderResources( SlotIndex, 1, &_pData );
		bUsed |= SlotIndex != -1;
	}
	{
		int	SlotIndex = m_GSConstants.GetShaderResourceViewIndex( _pBufferName );
		if ( SlotIndex != -1 )
			m_Device.DXContext().GSSetShaderResources( SlotIndex, 1, &_pData );
		bUsed |= SlotIndex != -1;
	}
	{
		int	SlotIndex = m_PSConstants.GetShaderResourceViewIndex( _pBufferName );
		if ( SlotIndex != -1 )
			m_Device.DXContext().PSSetShaderResources( SlotIndex, 1, &_pData );
		bUsed |= SlotIndex != -1;
	}

	return	bUsed;
}

static void	DeleteBindingDescriptors( Material::ShaderConstants::BindingDesc*& _pValue, void* _pUserData )
{
	delete _pValue;
}
Material::ShaderConstants::~ShaderConstants()
{
	m_ConstantBufferName2Descriptor.ForEach( DeleteBindingDescriptors, NULL );
	m_TextureName2Descriptor.ForEach( DeleteBindingDescriptors, NULL );
}
void	Material::ShaderConstants::Enumerate( ID3DBlob& _ShaderBlob )
{
	ID3D11ShaderReflection*	pReflector = NULL; 
	D3DReflect( _ShaderBlob.GetBufferPointer(), _ShaderBlob.GetBufferSize(), IID_ID3D11ShaderReflection, (void**) &pReflector );

	D3D11_SHADER_DESC	ShaderDesc;
	pReflector->GetDesc( &ShaderDesc );

	// Enumerate bound resources
	for ( int ResourceIndex=0; ResourceIndex < int(ShaderDesc.BoundResources); ResourceIndex++ )
	{
		D3D11_SHADER_INPUT_BIND_DESC	BindDesc;
		pReflector->GetResourceBindingDesc( ResourceIndex, &BindDesc );

		BindingDesc**	ppDesc = NULL;
		switch ( BindDesc.Type )
		{
		case D3D_SIT_TEXTURE:
			ppDesc = &m_TextureName2Descriptor.Add( BindDesc.Name );
			break;

		case D3D_SIT_CBUFFER:
			ppDesc = &m_ConstantBufferName2Descriptor.Add( BindDesc.Name );
			break;
		}
		if ( ppDesc == NULL )
			continue;	// We're not interested in that type !

		*ppDesc = new BindingDesc();
		(*ppDesc)->SetName( BindDesc.Name );
		(*ppDesc)->Slot = BindDesc.BindPoint;
	}

	pReflector->Release();
}

void	Material::ShaderConstants::BindingDesc::SetName( const char* _pName )
{
	int		NameLength = strlen(_pName)+1;
	pName = new char[NameLength];
	strcpy_s( pName, NameLength+1, _pName );
}
Material::ShaderConstants::BindingDesc::~BindingDesc()
{
//	delete[] pName;	// This makes a heap corruption, I don't know why and I don't give a fuck about these C++ problems... (certainly some shit about allocating memory from a DLL and releasing it from another one or something like this)
}

int		Material::ShaderConstants::GetConstantBufferIndex( const char* _pBufferName ) const
{
	BindingDesc**	ppValue = m_ConstantBufferName2Descriptor.Get( _pBufferName );
	return ppValue != NULL ? (*ppValue)->Slot : -1;
}

int		Material::ShaderConstants::GetShaderResourceViewIndex( const char* _pTextureName ) const
{
	BindingDesc**	ppValue = m_TextureName2Descriptor.Get( _pTextureName );
	return ppValue != NULL ? (*ppValue)->Slot : -1;
}
#endif

//////////////////////////////////////////////////////////////////////////
// Shader rebuild on modifications mechanism...
#ifdef _DEBUG

#include <sys/types.h>
#include <sys/stat.h>

DictionaryString<Material*>	Material::ms_WatchedShaders;

static void	WatchShader( Material*& _Value, void* _pUserData ) { _Value->WatchShaderModifications(); }
void		Material::WatchShadersModifications()
{
	static int	LastTime = -1;
	int			CurrentTime = timeGetTime();
	if ( LastTime >= 0 && (CurrentTime - LastTime) < REFRESH_CHANGES_INTERVAL )
		return;	// Too soon to check !

	// Update last check time
	LastTime = CurrentTime;

	ms_WatchedShaders.ForEach( WatchShader, NULL );
}

void		Material::WatchShaderModifications()
{
	// Check if the shader file changed since last time
	time_t	LastModificationTime = GetFileModTime( m_pShaderFileName );
	if ( LastModificationTime <= m_LastShaderModificationTime )
		return;	// No change !

	m_LastShaderModificationTime = LastModificationTime;

	// Reload file
	FILE*	pFile = NULL;
	fopen_s( &pFile, m_pShaderFileName, "rb" );
	ASSERT( pFile != NULL, "Failed to open shader file !" );

	fseek( pFile, 0, SEEK_END );
	size_t	FileSize = ftell( pFile );
	fseek( pFile, 0, SEEK_SET );

	char*	pShaderCode = new char[FileSize+1];
	fread_s( pShaderCode, FileSize, 1, FileSize, pFile );
	pShaderCode[FileSize] = '\0';

	fclose( pFile );

	// Compile
	CompileShaders( pShaderCode );

	delete[] pShaderCode;
}

time_t		Material::GetFileModTime( const char* _pFileName )
{	
	struct _stat statInfo;
	_stat( _pFileName, &statInfo );

	return statInfo.st_mtime;
}

#endif