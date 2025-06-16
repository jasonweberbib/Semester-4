Shader "CustomWorldSpaceTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale ("Texture Scale", Float) = 5.0
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Scale;
            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Determine which axis to use based on the world normal
                float3 worldNormal = normalize(abs(i.worldNormal));
                float2 uv;
                
                // Use the dominant axis for UV mapping
                if (worldNormal.x > worldNormal.y && worldNormal.x > worldNormal.z)
                {
                    // X-axis dominant (YZ plane)
                    uv = i.worldPos.yz * _Scale;
                }
                else if (worldNormal.y > worldNormal.z)
                {
                    // Y-axis dominant (XZ plane)
                    uv = i.worldPos.xz * _Scale;
                }
                else
                {
                    // Z-axis dominant (XY plane)
                    uv = i.worldPos.xy * _Scale;
                }
                
                // Sample the texture
                fixed4 col = tex2D(_MainTex, uv) * _Color;
                
                // Simple Lambert lighting to prevent the "too bright" look
                float3 normalizedNormal = normalize(i.worldNormal);
                float NdotL = max(0, dot(normalizedNormal, _WorldSpaceLightPos0.xyz));
                float lighting = NdotL * 0.8 + 0.2; // 80% directional + 20% ambient
                
                col.rgb *= lighting;
                
                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            ENDCG
        }
    }
}