Shader "Custom/Shdbilduing"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Scale ("Texture Scale", Float) = 1.0
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _Scale;
        half _Metallic;
        half _Smoothness;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // World position normalisieren für UV-Koordinaten
            float3 worldPos = IN.worldPos * _Scale;
            
            // Bestimme welche Achse die dominante ist für die UV-Projektion
            float3 absNormal = abs(IN.worldNormal);
            
            fixed4 texColor;
            
            // Projiziere Textur basierend auf der stärksten Normal-Komponente
            if (absNormal.x > absNormal.y && absNormal.x > absNormal.z)
            {
                // X-Achse dominant (Seiten)
                texColor = tex2D(_MainTex, worldPos.yz);
            }
            else if (absNormal.y > absNormal.z)
            {
                // Y-Achse dominant (Oben/Unten)
                texColor = tex2D(_MainTex, worldPos.xz);
            }
            else
            {
                // Z-Achse dominant (Vorne/Hinten)
                texColor = tex2D(_MainTex, worldPos.xy);
            }
            
            // Anwenden der Farbe und Textur
            o.Albedo = texColor.rgb * _Color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = _Color.a;
        }
        ENDCG
    }
    
    FallBack "Diffuse"
}