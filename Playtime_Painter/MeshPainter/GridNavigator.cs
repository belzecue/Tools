﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Painter;

public enum Gridside { xz, xy, zy }

public class GridNavigator : MonoBehaviour {
    public static GridNavigator inst()  {
        if (_inst == null) {
            _inst = FindObjectOfType<GridNavigator>();
            if (_inst == null) 
                _inst = Instantiate((Resources.Load("prefabs/grid") as GameObject)).GetComponent<GridNavigator>();

            _inst.transform.parent = PainterManager.inst.transform;
         }
        return _inst;
    }

    public Material vertexPointMaterial;
    public GameObject vertPrefab;
    public MarkerWithText[] verts;
    public MarkerWithText pointedVertex;
    public MarkerWithText selectedVertex;

    static GridNavigator _inst;



    public static Plane xzPlane = new Plane(Vector3.up, 0);
    public static Plane zyPlane = new Plane(Vector3.right, 0);
    public static Plane xyPlane = new Plane(Vector3.forward, 0);

    public static Quaternion xgrid = Quaternion.Euler(new Vector3(0, 90, 0));
    public static Quaternion zgrid = Quaternion.Euler(new Vector3(0, 0, 0));
    public static Quaternion ygrid = Quaternion.Euler(new Vector3(90, 0, 0));




    public static Vector3 collisionPos;
 
    public static Vector3 onGridPos;
    [HideInInspector]
    public Gridside g_side = Gridside.xz;
    public float UVsnapToPixelPortion = 2;
    public MeshRenderer dot;
    public MeshRenderer grid;




    public void UpdateVertColor()
    {
        Color col = Color.gray;

        switch (MeshManager._meshTool())
        {
            case MeshTool.vertices: col = Color.yellow; break;
            case MeshTool.uv: col = Color.magenta; break;
            case MeshTool.VertColor: col = Color.white; break;
        }

        vertexPointMaterial.SetColor("_Color", col);
    }

   public  void Deactivateverts()
    {
        for (int i = 0; i < MeshManager.inst().vertsShowMax; i++)
            verts[i].go.SetActive(false);

        pointedVertex.go.SetActive(false);
        selectedVertex.go.SetActive(false);
        // for (int i = 0; i < uvMarker.Count; i++ )
        //     uvMarker[i].SetActive(false);
    }

    public void SetEnabled(bool gridEn, bool dotEn) {
        //if (grid.enabled != gridEn)
            grid.EnabledUpdate(gridEn);
        //if (dot.enabled != dotEn)
            dot.EnabledUpdate(dotEn);
    }



   

    float AngleClamp(Quaternion ang)
    {


        float res = Quaternion.Angle(gameObject.tryGetCameraTransform().rotation, ang);
        if (res > 90)
            res = 180 - res;
        return res;
    }

    public float angGridToCamera(Vector3 hitpos)
    {
        float ang = (Vector3.Angle(getGridPerpendicularVector(), hitpos - gameObject.tryGetCameraTransform().position));
        if (ang > 90)
            ang = 180 - ang;
        return ang;
    }

    public static Vector3 MouseToPlane(Plane _plane)
    {
        Ray ray = EditorInputManager.GetScreenRay();
        float rayDistance;
        if (_plane.Raycast(ray, out rayDistance))
        {
            return ray.GetPoint(rayDistance);
        }
        else return Vector3.zero;
    }

    public Vector3 PlaneToWorldVector(Vector2 v2)
    {
        switch (g_side)
        {
            case Gridside.xy: return new Vector3(v2.x, v2.y, 0); //Mirror.z = 1; break;
            case Gridside.xz: return new Vector3(v2.x, 0, v2.y); //
            case Gridside.zy: return new Vector3(0, v2.y, v2.x); //
        }
        return Vector3.zero;

    }

    public Vector2 InPlaneVector(Vector3 f)
    {
        switch (g_side)
        {
            case Gridside.xy: return new Vector2(f.x, f.y); //Mirror.z = 1; break;
            case Gridside.xz: return new Vector2(f.x, f.z); //
            case Gridside.zy: return new Vector2(f.z, f.y); //
        }
        return Vector3.zero;
    }

    public Vector3 getGridPerpendicularVector()
    {
        Vector3 Mirror = new Vector3();
        switch (g_side)
        {
            case Gridside.xy: Mirror.z = 1; break;
            case Gridside.xz: Mirror.y = 1; break;
            case Gridside.zy: Mirror.x = 1; break;
        }
        return Mirror;
    }

    public Vector3 getGridMaskVector()
    {
        Vector3 Mirror = Vector3.one;
        switch (g_side)
        {
            case Gridside.xy: Mirror.z = 0; break;
            case Gridside.xz: Mirror.y = 0; break;
            case Gridside.zy: Mirror.x = 0; break;
        }
        return Mirror;
    }

    public Vector3 ProjectToGrid(Vector3 src)
    {
        Vector3 pos = onGridPos;
        switch (g_side)
        {
            case Gridside.xy:
                return new Vector3(src.x, src.y, pos.z);
            case Gridside.xz:
                return new Vector3(src.x, pos.y, src.z);
            case Gridside.zy:
                return new Vector3(pos.x, src.y, src.z);
        }
        return Vector3.zero;
    }

    public void ClosestAxis(bool horToo)
    {
        float ang = gameObject.tryGetCameraTransform().rotation.x;
        if ((!horToo) || (ang < 35 || ang > 300))
        {
            float x = AngleClamp(xgrid);
            float z = AngleClamp(zgrid);

            if (x <= z) g_side = Gridside.zy;
            else g_side = Gridside.xy;
            //  Debug.Log("X " + x + " Z " + z + " result " + g_side.ToString());
        }
        else g_side = Gridside.xz;

    }
    public bool ScrollsProcess(float delta)
    {

        if (delta > 0)
        {

            switch (g_side)
            {
                case Gridside.xy: g_side = Gridside.zy; break;
                case Gridside.xz: ClosestAxis(false); break;
                case Gridside.zy: g_side = Gridside.xy; break;
            }
  
        }
        else if (delta < 0)
        {
            g_side = Gridside.xz;
          
        }

        return delta != 0;

    }



    public void UpdatePositions() {
        playtimeMesherSaveData sd = MeshManager.cfg;

        if (sd.SnapToGridSize <= 0) sd.SnapToGridSize = 1;

        switch (g_side)
        {
            case Gridside.xy: grid.transform.rotation = zgrid; break;
            case Gridside.xz: grid.transform.rotation = ygrid; break;
            case Gridside.zy: grid.transform.rotation = xgrid; break;
        }

        xzPlane.distance = -onGridPos.y;
        xyPlane.distance = -onGridPos.z;
        zyPlane.distance = -onGridPos.x;

        Vector3 hit = Vector3.zero;
        switch (g_side)    {
            case Gridside.xy:   hit = MouseToPlane(xyPlane);           break;
            case Gridside.xz:   hit = MouseToPlane(xzPlane);           break;
            case Gridside.zy:   hit = MouseToPlane(zyPlane);           break;
        }

        if (sd.SnapToGrid)
            hit = MyMath.RoundDiv(hit, sd.SnapToGridSize);

        if (hit != Vector3.zero)  {

            switch (g_side)
            {
                case Gridside.xy:

                    onGridPos.x = hit.x;
                    onGridPos.y = hit.y;

                    break;
                case Gridside.xz:
                    onGridPos.x = hit.x;
                    onGridPos.z = hit.z;
                    break;
                case Gridside.zy:

                    onGridPos.z = hit.z;
                    onGridPos.y = hit.y;
                    break;
            }
        }

        transform.position = onGridPos+Vector3.one*0.01f;

        Shader.SetGlobalVector("_GridDotPosition", new Vector4(onGridPos.x, onGridPos.y, onGridPos.z));
        // Set grid direction to cast light only on this side of the grid.
        //Shader.SetGlobalVector("_GridDirection", new Vector4(onGridPos.x, onGridPos.y, onGridPos.z));

        dot.transform.rotation = gameObject.tryGetCameraTransform().rotation;

        Transform cam = gameObject.tryGetCameraTransform();

        float dist = Mathf.Max(0.1f, (cam.position - transform.position).magnitude * 2);

        dot.transform.localScale = Vector3.one * (dist / 64f);
        grid.transform.localScale = new Vector3(dist, dist, dist);



        float dx = 0;
        float dy = 0;

        if (g_side != Gridside.zy)
            dx = (transform.position.x);
        else dx = (-transform.position.z);

        if (g_side != Gridside.xz)
            dy = (transform.position.y);
        else dy = (transform.position.z);

        float scale = 8;
        if (!MeshManager.cfg.SnapToGrid)
            scale = Mathf.Max(1, Mathf.ClosestPowerOfTwo((int)(dist / 8)));
        else scale = MeshManager.cfg.SnapToGridSize;

        dx -= Mathf.Round(dx / scale) * scale;
        dy -= Mathf.Round(dy / scale) * scale;
        grid.sharedMaterial.SetFloat("_dx", dx / dist);
        grid.sharedMaterial.SetFloat("_dy", dy / dist);
        grid.sharedMaterial.SetFloat("_Size", dist / scale);
    }

    private void OnEnable() {
        _inst = this;
    }
   



}
