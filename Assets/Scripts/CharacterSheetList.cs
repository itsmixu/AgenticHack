using System.Collections.Generic;
using UnityEngine;

public class CharacterSheetList : MonoBehaviour
{
    public static CharacterSheetList Instance;

    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject charSheetPrefab;
    [SerializeField] private GameObject canvas;

    public List<CharacterSheet> itemsList = new();

    private Transform slotsParent;
    private CharSheetUI[] slots;
    private int listCount;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(canvas);
        }
        else
        {
            Destroy(canvas);
        }
    }


    public void Start()
    {
        slotsParent = transform;
        
        // Create slots in UI
        slots = new CharSheetUI[14];
        for (int i = 0; i < slots.Length; i++)
        {
            GameObject slotGO = Instantiate(charSheetPrefab, slotsParent);
            slots[i] = slotGO.GetComponent<CharSheetUI>();
            slots[i].ClearSlot();
        }
    }

    public void AddToList(CharacterSheet item)
    {
        if (!itemsList.Contains(item))
        {
            if (listCount < slots.Length) // Prevent overflow
            {
                slots[listCount].SetItem(item);
                itemsList.Add(item);
                listCount++;
            }
        }
    }
}
