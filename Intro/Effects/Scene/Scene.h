//////////////////////////////////////////////////////////////////////////
// This is the main scene manager that handles objects & their primitives
// Each primitive is part of an object and is tied to a bunch of complex materials
// 
#pragma once

template<typename> class CB;
class	MaterialBank;

class EffectScene
{
private:	// CONSTANTS

public:		// NESTED TYPES

	class	Object
	{
	public:		// NESTED TYPES

		class	Primitive
		{
		public:		// NESTED TYPES

// 			struct	MaterialParameters
// 			{
// 				Texture2D*		pTextures;
// 				unsigned int	MatIDs[4];
// 				NjFloat4		Thickness;
// 				NjFloat3		Extinction;
// 				NjFloat3		IOR;
// 			};

			struct	CBPrimitive
			{
				unsigned int	MatIDs[4];	// 4 material IDs in [0,255] from the material bank, one for each layer of the primitive
				float4		Thickness;	// The thickness of the 4 layers, in millimeters. Thickness of layer 0 serves as multiplier for the height map.
				float3		Extinction;	// The extinction coefficients of the top 3 layers
				float			__Pad1;
				float3		IOR;		// The IOR of the top 3 layers
				float			__Pad2;
				float3		Frosting;	// The frosting coefficient of the top 3 layers
				float			__Pad3;
				float4		NoDiffuse;	// The "no diffuse" coefficient of the 4 layers

				// TODO: Add tiling + offset for each layer
			};

		protected:	// FIELDS

			Object&				m_Owner;

			CB<CBPrimitive>*	m_pCB_Primitive;
			::Primitive*		m_pPrimitive;		// Actual renderable primitive
			Texture2D*			m_pTextures;		// Texture2DArray with 4 layers + normal + specular

		public:		// PROPERTIES

			const Texture2D*	GetLayeredTexture() const	{ return m_pTextures; }

		public:		// METHODS

			Primitive( Object& _Owner );
			~Primitive();

			void	Render( Shader& _Material, bool _bDepthPass=false ) const;

			void	SetRenderPrimitive( ::Primitive& _Primitive );
//			void	SetMaterial( MaterialParameters& _Material );
			void	SetLayerMaterials( Texture2D& _LayeredTextures, int _Mat0, int _Mat1, int _Mat2, int _Mat3 );
		};

		struct	CBObject
		{
			float4x4	Local2World;
		};

	protected:		// FIELDS

		EffectScene&	m_Owner;
		const char*		m_pName;

		bool			m_bPRSDirty;
		float3		m_Position;
		float4		m_Rotation;	// Rotation as a quaternion
		float3		m_Scale;

		CB<CBObject>*	m_pCB_Object;

		// The object's primitives
		int				m_PrimitivesCount;
		Primitive**		m_ppPrimitives;

	public:		// METHODS

		Object( EffectScene& _Owner, const char* _pName );
		~Object();

		void		SetPRS( const float3& _Position, const float4& _Rotation, const float3& _Scale=float3::One );

		void		Update( float _Time, float _DeltaTime );
		void		Render( Shader& _Material, bool _bDepthPass=false ) const;

		// Primitives management
		void		AllocatePrimitives( int _PrimitivesCount );
		void		DestroyPrimitives();
		Primitive&	GetPrimitiveAt( int _PrimitiveIndex );
	};

	class	Light
	{
	public:		// FIELDS

		bool		m_bEnabled;

		float3	m_Position;
		float3	m_Direction;
		float3	m_Radiance;

		union
		{
			// DIRECTIONAL
			struct
			{
				float		m_RadiusHotSpot;	// Radius of the hotspot
				float		m_RadiusFalloff;	// Radius of the falloff
				float		m_Length;			// Length of the directional
			};

			// POINT LIGHTS
			struct
			{
				float		m_Radius;			// Radius of influence
			};

			// SPOTS
			struct
			{
				float		m_AngleHotSpot;		// Angle of the hotspot
				float		m_AngleFalloff;		// Angle of the falloff
				float		m_Length;			// Length of the spot
			};
		} m_Data;

	public:		// METHODS

		Light();

		void	SetDirectional( const float3& _Irradiance, const float3& _Position, const float3& _Direction, float _RadiusHotSpot, float _RadiusFalloff, float _Length );
		void	SetPoint( const float3& _Radiance, const float3& _Position, float _Radius );
		void	SetSpot( const float3& _Radiance, const float3& _Position, const float3& _Direction, float _AngleHotspot, float _AngleFalloff, float _Length );
	};

private:	// FIELDS

	Device&			m_Device;

	int				m_ObjectsCount;
	Object**		m_ppObjects;

	int				m_LightsCountDirectional;
	int				m_EnabledLightsCountDirectional;
	Light*			m_pLightsDirectional;
	int				m_LightsCountPoint;
	int				m_EnabledLightsCountPoint;
	Light*			m_pLightsPoint;
	int				m_LightsCountSpot;
	int				m_EnabledLightsCountSpot;
	Light*			m_pLightsSpot;

	Texture2D*		m_pTexEnvMap;

	MaterialBank*	m_pMaterials;


public:		// PROPERTIES

	MaterialBank&		GetMaterialBank()	{ return *m_pMaterials; }

	int					GetDirectionalLightsCount() const			{ return m_LightsCountDirectional; }
	int					GetEnabledDirectionalLightsCount() const	{ return m_EnabledLightsCountDirectional; }
	const Light*		GetDirectionalLights() const				{ return m_pLightsDirectional; }
	int					GetPointLightsCount() const					{ return m_LightsCountPoint; }
	int					GetEnabledPointLightsCount() const			{ return m_EnabledLightsCountPoint; }
	const Light*		GetPointLights() const						{ return m_pLightsPoint; }
	int					GetSpotLightsCount() const					{ return m_LightsCountSpot; }
	int					GetEnabledSpotLightsCount() const			{ return m_EnabledLightsCountSpot; }
	const Light*		GetSpotLights() const						{ return m_pLightsSpot; }

	const Texture2D*	GetEnvMap() const							{ return m_pTexEnvMap; }

public:		// METHODS

	EffectScene( Device& _Device );
	~EffectScene();

	void		Update( float _Time, float _DeltaTime );
	void		Render( Shader& _Material, bool _bDepthPass=false ) const;

	// Objects management
	void		AllocateObjects( int _ObjectsCount );
	void		DestroyObjects();
	Object&		CreateObjectAt( int _ObjectIndex, const char* _pName );
	Object&		GetObjectAt( int _ObjectIndex );

	// Lights management
	void		AllocateLights( int _DirectionalsCount, int _PointsCount, int _SpotsCount );
	void		DestroyLights();
	Light&		GetDirectionalLightAt( int _LightIndex );
	Light&		GetPointLightAt( int _LightIndex );
	Light&		GetSpotLightAt( int _LightIndex );
	void		SetDirectionalLightEnabled( int _LightIndex, bool _bEnabled );
	void		SetPointLightEnabled( int _LightIndex, bool _bEnabled );
	void		SetSpotLightEnabled( int _LightIndex, bool _bEnabled );

	void		SetEnvMap( Texture2D& _EnvMap );
};


// typedef	EffectScene::Object::Primitive::MaterialParameters	PrimitiveMaterial;
