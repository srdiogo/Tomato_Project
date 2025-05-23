using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutoutObject : MonoBehaviour
{
    [SerializeField]
    private LayerMask wallMask;

    [SerializeField]
    private Camera mainCamera;

    // Material que será aplicado ao objeto ocluído
    [SerializeField]
    private Material wallMaterial;

    // Controle do tamanho do raio e do offset
    [SerializeField]
    private float raycastDistance = 10f;  // Tamanho do raio do Raycast

    [SerializeField]
    private Vector3 raycastOffset = Vector3.zero; // Offset do Raycast

    // Dicionário para armazenar os materiais originais dos objetos
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        Vector2 cutoutPos = mainCamera.WorldToViewportPoint(transform.position);
        cutoutPos.y /= (Screen.width / Screen.height);

        // Define a direção do Raycast com o offset
        Vector3 direction = (transform.position + raycastOffset) - mainCamera.transform.position;

        // Raycast para detectar objetos com a distância ajustável
        RaycastHit[] hitObjects = Physics.RaycastAll(mainCamera.transform.position, direction, raycastDistance, wallMask);

        // Debug do Raycast
        Debug.DrawRay(mainCamera.transform.position, direction.normalized * raycastDistance, Color.red);

        // Manter o controle de quais objetos colidimos
        List<Renderer> hitRenderers = new List<Renderer>();

        for (int i = 0; i < hitObjects.Length; ++i)
        {
            Renderer renderer = hitObjects[i].transform.GetComponent<Renderer>();
            if (renderer != null)
            {
                hitRenderers.Add(renderer);

                // Se ainda não armazenamos os materiais originais, fazemos isso agora
                if (!originalMaterials.ContainsKey(renderer))
                {
                    originalMaterials[renderer] = renderer.materials;
                }

                Material[] materials = renderer.materials;

                for (int m = 0; m < materials.Length; ++m)
                {
                    // Pega a textura original
                    Texture originalTexture = materials[m].mainTexture;

                    // Aplica o WallMaterial mantendo a textura original
                    materials[m] = new Material(wallMaterial);
                    materials[m].mainTexture = originalTexture;

                    // Definindo os parâmetros do cutout
                    materials[m].SetVector("_CutoutPos", cutoutPos);
                    materials[m].SetFloat("_CutoutSize", 0.1f);
                    materials[m].SetFloat("_FalloffSize", 0.05f);
                }

                // Aplicar os materiais modificados ao objeto
                renderer.materials = materials;
            }
        }

        // Restaurar os materiais dos objetos que não estão mais sendo atingidos pelo Raycast
        List<Renderer> renderersToRestore = new List<Renderer>(originalMaterials.Keys);
        renderersToRestore.RemoveAll(hitRenderers.Contains);

        foreach (Renderer renderer in renderersToRestore)
        {
            if (renderer != null && originalMaterials.ContainsKey(renderer))
            {
                // Restaurar os materiais originais
                renderer.materials = originalMaterials[renderer];
                originalMaterials.Remove(renderer); // Remover do dicionário
            }
        }
    }
}
