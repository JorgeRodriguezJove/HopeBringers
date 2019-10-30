﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndividualTiles : MonoBehaviour
{
    #region VARIABLES

    //Referencia al Tile Manager
    [HideInInspector]
    public TileManager TM;
    //[HideInInspector]
    public LevelManager LM;

    //Coordenadas del tile
    [HideInInspector]
    public int tileX;
    [HideInInspector]
    public int tileZ;

    //Altura del tile
    public int height;

    //Lista con los 4 vecinos adyacentes. Esto se usa para el pathfinding.
    [HideInInspector]
    public List<IndividualTiles> neighbours;

    //Coste que tiene una casilla.
    [SerializeField]    
    public int movementCost;

    //Variable que determina si una casilla se pueda andar sobre ella.
    [SerializeField]
    public bool isWalkable;

    //Unidad encima del tile
    [HideInInspector]
    public GameObject unitOnTile;

    //MATERIALES. CAMBIAR ESTO POR SHADERS

    [SerializeField]
    private Material colorTest;
    private Material initialColor;

    #endregion

    #region INIT

    private void Start()
    {
        initialColor = GetComponent<MeshRenderer>().material;
    }

    #endregion


    //Cambiar el color del tile
    public void ColorSelect()
    {
        GetComponent<MeshRenderer>().material = colorTest;
    }

    public void ColorDeselect()
    {
        GetComponent<MeshRenderer>().material = initialColor;
    }

    private void OnMouseUp()
    {
        LM.MoveUnit(this);
    }

}
