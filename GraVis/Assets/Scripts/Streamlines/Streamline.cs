using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using UnityEngine.Rendering;

[BurstCompile]
public struct CalcStreamlineJob : IJob
{
    [ReadOnly]
    public NativeArray<float> dataset;
    public NativeArray<float> seed;
    public float ClosureDelta;
    public float RKDelta;
    public NativeArray<int> SpaceDimension;
    public int sampleDim;
    public NativeArray<float> streamlinePoints;
    public NativeArray<float> backSideSP; // streamline points for the other direction
    public int maxLinePoints; // sets the array size
    public NativeArray<int> actualLinePoints; // Count of points, single element list

    [ReadOnly]
    public NativeArray<float> con_k; // Constants for the k values
    [ReadOnly]
    public NativeArray<float> con_y; // Constants for the evaluation of ki = f(xi,yi) (xi itself is constant)

    // RK Values
    float k10, k11, k12;
    float v10, v11, v12;
    float k20, k21, k22;
    float v20, v21, v22;
    float k30, k31, k32;
    float v30, v31, v32;
    float k40, k41, k42;
    float v40, v41, v42;
    float k50, k51, k52;
    float v50, v51, v52;
    float k60, k61, k62;
    float vR40, vR41, vR42;
    float vR50, vR51, vR52;
    float ME; //Mean error

    public int GetArrayPosition(float position0, float position1, float position2)
    {
        float index0, index1, index2;
        index0 = (position0 + 0.5f) * SpaceDimension[0];
        index1 = (position1 + 0.5f) * SpaceDimension[1];
        index2 = (position2 + 0.5f) * SpaceDimension[2];
        int arrayPos =
            (int)index0
            + SpaceDimension[0] * (int)index1
            + SpaceDimension[0] * SpaceDimension[1] * (int)index2;
        return arrayPos * sampleDim;
    }

    private NativeArray<float> GetSamplePointNN(float position0, float position1, float position2)
    {
        // NN
        int arrayPos = GetArrayPosition(position0, position1, position2);
        return dataset.GetSubArray(arrayPos, 3);
    }

    /// <summary>
    /// Calculates a tri-linear interpolation on the dataset at the given point.
    /// Writes into the given native array
    /// </summary>
    /// <param name="position0"></param>
    /// <param name="position1"></param>
    /// <param name="position2"></param>
    /// <returns></returns>
    private NativeArray<float> GetSamplePointTI(float position0, float position1, float position2, NativeArray<float> outArray)
    {

        // First, calculate the array positions from the world positions
        float p0, p1, p2;
        p0 = (position0 + 0.5f) * SpaceDimension[0];
        p1 = (position1 + 0.5f) * SpaceDimension[1];
        p2 = (position2 + 0.5f) * SpaceDimension[2];

        // check if position is within bounds
        if (p0 < 1.0f || p0 > SpaceDimension[0] - 1.0f ||
            p1 < 1.0f || p1 > SpaceDimension[1] - 1.0f ||
            p2 < 1.0f || p2 > SpaceDimension[2] - 1.0f)
        {
            outArray[0] = 0.0f;
            outArray[1] = 0.0f;
            outArray[2] = 0.0f;
            return outArray;
        }

        // Calculate the weights
        float wx, wy, wz;
        wx = p0 - (int)p0;
        wy = p1 - (int)p1;
        wz = p2 - (int)p2;

        // calculate the array position
        int arrPos = ((int)p0 + (int)p1 * SpaceDimension[0] + (int)p2 * SpaceDimension[0] * SpaceDimension[1]) * sampleDim;
        //Debug.Log(arrPos);
        // calculate x,y,z steps
        int xStep = sampleDim;
        int yStep = SpaceDimension[0] * sampleDim;
        int zStep = yStep * SpaceDimension[1];

        // Calculate the surrounding neighbors
        // We do the multiplications beforehand

        //int dimA = SpaceDimension[0] * sampleDim;
        //int dimB = SpaceDimension[1] * SpaceDimension[0] * sampleDim;

        //int pMin0 = (int)p0 * sampleDim;
        //int pMin1 = (int)p1 * dimA;
        //int pMin2 = (int)p2 * dimB;

        NativeArray<float> c000 = dataset.GetSubArray(arrPos, 3);
        NativeArray<float> c001 = dataset.GetSubArray(arrPos + zStep, 3);
        NativeArray<float> c010 = dataset.GetSubArray(arrPos + yStep, 3);
        NativeArray<float> c100 = dataset.GetSubArray(arrPos + xStep, 3);
        NativeArray<float> c101 = dataset.GetSubArray(arrPos + xStep + zStep, 3);
        NativeArray<float> c110 = dataset.GetSubArray(arrPos + xStep + yStep, 3);
        NativeArray<float> c111 = dataset.GetSubArray(arrPos + xStep + yStep + zStep, 3);
        NativeArray<float> c011 = dataset.GetSubArray(arrPos + yStep + zStep, 3);

        /*
        NativeArray<float> c000 = dataset.GetSubArray(pMin0 + pMin1 + pMin2, 3);
        NativeArray<float> c001 = dataset.GetSubArray(pMin0 + pMin1 + pMin2 + dimB, 3);
        NativeArray<float> c010 = dataset.GetSubArray(pMin0 + pMin1 + dimA + pMin2, 3);
        NativeArray<float> c100 = dataset.GetSubArray(pMin0 + 3 + pMin1 + pMin2, 3);
        NativeArray<float> c101 = dataset.GetSubArray(pMin0 + 3 + pMin1 + pMin2 + dimB, 3);
        NativeArray<float> c110 = dataset.GetSubArray(pMin0 + 3 + pMin1 + dimA + pMin2, 3);
        NativeArray<float> c111 = dataset.GetSubArray(pMin0 + 3 + pMin1 + dimA + pMin2 + dimB, 3);
        NativeArray<float> c011 = dataset.GetSubArray(pMin0 + pMin1 + dimA + dimB * pMin2 + dimB, 3);
        */
        // Calculate x-wise
        //c00 = (c000 * (1.0f - wx) + c100 * wx);
        //c01 = (c001 * (1.0f - wx) + c101 * wx);
        //c10 = (c010 * (1.0f - wx) + c110 * wx);
        //c11 = (c011 * (1.0f - wx) + c111 * wx);

        // y wise
        //c0 = ((c000 * (1.0f - wx) + c100 * wx) * (1.0f - wy) + (c010 * (1.0f - wx) + c110 * wx) * wy);
        //c1 = ((c001 * (1.0f - wx) + c101 * wx) * (1.0f - wy) + (c011 * (1.0f - wx) + c111 * wx) * wy);

        outArray[0] = (((c000[0] * (1.0f - wx) + c100[0] * wx) * (1.0f - wy) + (c010[0] * (1.0f - wx) + c110[0] * wx) * wy) * (1.0f - wz) 
            + ((c001[0] * (1.0f - wx) + c101[0] * wx) * (1.0f - wy) + (c011[0] * (1.0f - wx) + c111[0] * wx) * wy) * wz);
        outArray[1] = (((c000[1] * (1.0f - wx) + c100[1] * wx) * (1.0f - wy) + (c010[1] * (1.0f - wx) + c110[1] * wx) * wy) * (1.0f - wz)
            + ((c001[1] * (1.0f - wx) + c101[1] * wx) * (1.0f - wy) + (c011[1] * (1.0f - wx) + c111[1] * wx) * wy) * wz);
        outArray[2] = (((c000[2] * (1.0f - wx) + c100[2] * wx) * (1.0f - wy) + (c010[2] * (1.0f - wx) + c110[2] * wx) * wy) * (1.0f - wz)
            + ((c001[2] * (1.0f - wx) + c101[2] * wx) * (1.0f - wy) + (c011[2] * (1.0f - wx) + c111[2] * wx) * wy) * wz);

        return outArray;
    }
    
    private void RungeKuttaFehlberg(NativeArray<float> pt, float delta, float errorScale, float errorTolerance, NativeArray<float> writeArray, int index, out float force, out float newDelta)
    {
        float h = delta; // Stepsize
        
        NativeArray<float> dataPoint = new NativeArray<float>(3, Allocator.Temp);

        float errorTarget = errorTolerance * errorScale;

        do
        {
            GetSamplePointTI(pt[0], pt[1], pt[2], dataPoint);
            
            k10 = dataPoint[0];
            k11 = dataPoint[1];
            k12 = dataPoint[2];

            
            v10 = pt[0] + con_y[0] * k10 * h;
            v11 = pt[1] + con_y[0] * k11 * h;
            v12 = pt[2] + con_y[0] * k12 * h;
            GetSamplePointTI(v10, v11, v12, dataPoint);
            
            k20 = dataPoint[0];
            k21 = dataPoint[1];
            k22 = dataPoint[2];

            
            v20 = pt[0] + con_y[1] * k10 * h + con_y[2] * k20 * h;
            v21 = pt[1] + con_y[1] * k11 * h + con_y[2] * k21 * h;
            v22 = pt[2] + con_y[1] * k12 * h + con_y[2] * k22 * h;
            GetSamplePointTI(v20, v21, v22, dataPoint);
            
            k30 = dataPoint[0];
            k31 = dataPoint[1];
            k32 = dataPoint[2];

            
            v30 = pt[0] + con_y[3] * k10 * h + con_y[4] * k20 * h + con_y[5] * k30 * h;
            v31 = pt[1] + con_y[3] * k11 * h + con_y[4] * k21 * h + con_y[5] * k31 * h;
            v32 = pt[2] + con_y[3] * k12 * h + con_y[4] * k22 * h + con_y[5] * k32 * h;
            GetSamplePointTI(v30, v31, v32, dataPoint);
            
            k40 = dataPoint[0];
            k41 = dataPoint[1];
            k42 = dataPoint[2];

            
            v40 = pt[0] + con_y[6] * k10 * h + con_y[7] * k20 * h + con_y[8] * k30 * h + con_y[9] * k40 * h;
            v41 = pt[1] + con_y[6] * k11 * h + con_y[7] * k21 * h + con_y[8] * k31 * h + con_y[9] * k41 * h;
            v42 = pt[2] + con_y[6] * k12 * h + con_y[7] * k22 * h + con_y[8] * k32 * h + con_y[9] * k42 * h;
            GetSamplePointTI(v40, v41, v42, dataPoint);
            
            k50 = dataPoint[0];
            k51 = dataPoint[1];
            k52 = dataPoint[2];

            
            v50 = pt[0] + con_y[10] * k10 * h + con_y[11] * k20 * h + con_y[12] * k30 * h + con_y[13] * k40 * h + con_y[14] * k50 * h;
            v51 = pt[1] + con_y[10] * k11 * h + con_y[11] * k21 * h + con_y[12] * k31 * h + con_y[13] * k41 * h + con_y[14] * k51 * h;
            v52 = pt[2] + con_y[10] * k12 * h + con_y[11] * k22 * h + con_y[12] * k32 * h + con_y[13] * k42 * h + con_y[14] * k52 * h;
            GetSamplePointTI(v50, v51, v52, dataPoint);
            
            k60 = dataPoint[0];
            k61 = dataPoint[1];
            k62 = dataPoint[2];

            vR40 = h * (con_k[0] * k10 + con_k[1] * k30 + con_k[2] * k40 + con_k[3] * k60);
            vR41 = h * (con_k[0] * k11 + con_k[1] * k31 + con_k[2] * k41 + con_k[3] * k61);
            vR42 = h * (con_k[0] * k12 + con_k[1] * k32 + con_k[2] * k42 + con_k[3] * k62);

            vR50 = h * (con_k[4] * k10 + con_k[5] * k30 + con_k[6] * k40 + con_k[7] * k50 + con_k[8] * k60);
            vR51 = h * (con_k[4] * k11 + con_k[5] * k31 + con_k[6] * k41 + con_k[7] * k51 + con_k[8] * k61);
            vR52 = h * (con_k[4] * k12 + con_k[5] * k32 + con_k[6] * k42 + con_k[7] * k52 + con_k[8] * k62);

            ME = Mathf.Abs(vR40 - vR50) + Mathf.Abs(vR41 - vR51) + Mathf.Abs(vR42 - vR52);
            ME /= 3.0f;
            //Debug.Log(ME);
            //Debug.Log(h);
            if (ME > errorTarget) // Error is too big -> decrease h and retry
            {
                h = 0.9f * h * Mathf.Pow(errorTarget / ME, 0.2f);
                if (Mathf.Abs(h) < 0.0001f)
                {
                    h = Mathf.Sign(h) * 0.0001f;
                    newDelta = h;
                    break;
                }
                //Debug.Log(h.ToString());
            }
            else // Error is very small, we can safely increase h and go to the next iteration
            {
                newDelta = h;
                // we define a maximum step size of 0.1f;
                if (ME != 0)
                    newDelta *= Mathf.Pow(errorTarget / ME, 0.2f);
                break;
            }

        } while (true);

        writeArray[index] = pt[0] + vR40;
        writeArray[index + 1] = pt[1] + vR41;
        writeArray[index + 2] = pt[2] + vR42;

        force = Mathf.Sqrt(vR40 * vR40 + vR41 * vR41 + vR42 * vR42);
        dataPoint.Dispose();
    }

    private void RungeKutta4(NativeArray<float> pt, float delta, NativeArray<float> writeArray, int index, out float force) // Use delta < 1/2 Pixelsize
    {
        NativeArray<float> dataPoint = new NativeArray<float>(3, Allocator.Temp);

        //dataPoint = GetSamplePointNN(pt[0], pt[1], pt[2]);
        GetSamplePointTI(pt[0], pt[1], pt[2], dataPoint);
        float k10, k11, k12;
        k10 = dataPoint[0];
        k11 = dataPoint[1];
        k12 = dataPoint[2];

        float v10, v11, v12;
        v10 = pt[0] + 0.5f * k10 * delta;
        v11 = pt[1] + 0.5f * k11 * delta;
        v12 = pt[2] + 0.5f * k12 * delta;
        //dataPoint = GetSamplePointNN(v10, v11, v12);
        GetSamplePointTI(v10, v11, v12, dataPoint);
        float k20, k21, k22;
        k20 = dataPoint[0];
        k21 = dataPoint[1];
        k22 = dataPoint[2];

        float v20, v21, v22;
        v20 = pt[0] + 0.5f * k20 * delta;
        v21 = pt[1] + 0.5f * k21 * delta;
        v22 = pt[2] + 0.5f * k22 * delta;
        //dataPoint = GetSamplePointNN(v20, v21, v22);
        GetSamplePointTI(v20, v21, v22, dataPoint);
        float k30, k31, k32;
        k30 = dataPoint[0];
        k31 = dataPoint[1];
        k32 = dataPoint[2];

        float v30, v31, v32;
        v30 = pt[0] + k30 * delta;
        v31 = pt[1] + k31 * delta;
        v32 = pt[2] + k32 * delta;
        //dataPoint = GetSamplePointNN(v30, v31, v32);
        GetSamplePointTI(v30, v31, v32, dataPoint);
        float k40, k41, k42;
        k40 = dataPoint[0];
        k41 = dataPoint[1];
        k42 = dataPoint[2];

        float v1 = 1.0f / 6.0f * (k10 + 2.0f * k20 + 2.0f * k30 + k40) * delta;
        float v2 = 1.0f / 6.0f * (k11 + 2.0f * k21 + 2.0f * k31 + k41) * delta;
        float v3 = 1.0f / 6.0f * (k12 + 2.0f * k22 + 2.0f * k32 + k42) * delta;

        writeArray[index] = pt[0] + v1;
        writeArray[index + 1] = pt[1] + v2;
        writeArray[index + 2] = pt[2] + v3;

        force = v1*v1 + v2*v2 + v3*v3;
        dataPoint.Dispose();
    }

    private float distance2(NativeArray<float> pt1, NativeArray<float> pt2)
    {
        return (pt1[0] - pt2[0]) * (pt1[0] - pt2[0])
            + (pt1[1] - pt2[1]) * (pt1[1] - pt2[1])
            + (pt1[2] - pt2[2]) * (pt1[2] - pt2[2]);
    }

    public void ReComputeStreamlinesCPU()
    {
        // We have two arrays: s+ and s- for each direction
        // We keep the strength of the last calculated vector of each array
        // With that, we chose which step needs to be calculated next (s+ or s-) until maxLinePoints is reached
        // Also, we keep the distance of the last calculated points of s+ and s-. 
        // With them, we can track if the distance is growing or widening and therefore break the iteration if a minimum is reached
        // After the iterations, we must concatenate the arrays
        // 

        //streamlinePoints = new NativeArray<float>(maxLinePoints, Allocator.Persistent);
        streamlinePoints[0] = seed[0];
        streamlinePoints[1] = seed[1];
        streamlinePoints[2] = seed[2];

        backSideSP[0] = seed[0];
        backSideSP[1] = seed[1];
        backSideSP[2] = seed[2];
        float max = 0;
        float ptDistance = 0.0f;           // Distance of the two points
        float ptDistancePrevious = 0.0f;   // prev. distld)
        bool checkForLoop = false;
        float forceP1 = float.MaxValue;
        float forceP2 = float.MaxValue;
        int indexP1 = 3;
        int indexP2 = 3;
        bool P1Range = true;
        bool P2Range = true;
        int P1Size = 1;
        int P2Size = 1;

        float h_positive = RKDelta;
        float h_negative = -RKDelta;

        float errorTolerance = 1.0e-8f;
        float errorScale = 1.0f;

        float minimumForce = 1.0e-7f;

        // We first estimate the initial delta to prevent early exit
        NativeArray<float> seedVector = new NativeArray<float>(3, Allocator.Temp);
        GetSamplePointTI(seed[0], seed[1], seed[2], seedVector);
        float length = Mathf.Sqrt(seedVector[0] * seedVector[0] + seedVector[1] * seedVector[1] + seedVector[2] * seedVector[2]);
        if (length > 0)
            h_positive = 1.0f / length;
        h_negative = -h_positive;
        seedVector.Dispose();
        for (int i = 1; i < maxLinePoints; i++)
        {
            // ADD ARRAY ELEMENTS
            // We always add elements on the side, where the force is higher

            if (P1Range && forceP1 >= forceP2) // ergänze im positiven Array
            {
                //Debug.Log(forceP1);
                RungeKuttaFehlberg(
                    streamlinePoints.GetSubArray(indexP1 - 3, 3), 
                    h_negative, 
                    errorScale, 
                    errorTolerance, 
                    streamlinePoints, 
                    indexP1, 
                    out forceP1, 
                    out h_negative);
                //RungeKutta4(streamlinePoints.GetSubArray(indexP1 - 3, 3), -RKDelta, streamlinePoints, indexP1, out forceP1)

                // Range check
                if (streamlinePoints[indexP1] > 0.5f || streamlinePoints[indexP1] < -0.5f
                || streamlinePoints[indexP1 + 1] > 0.5f || streamlinePoints[indexP1 + 1] < -0.5f
                || streamlinePoints[indexP1 + 2] > 0.5f || streamlinePoints[indexP1 + 2] < -0.5f)
                {
                    //Debug.Log("Negative out of range ");
                    P1Range = false;
                }
                // Check size of power and use P1Range variable
                else if (forceP1 < minimumForce)
                {
                    //Debug.Log("Negative force too small " + forceP1);
                    P1Range = false;
                }
                else
                {
                    indexP1 += 3;
                    P1Size++;
                }
                
            }
            else if (P2Range)// ergänze im negativen Array
            {
                RungeKuttaFehlberg(
                    backSideSP.GetSubArray(indexP2 - 3, 3), 
                    h_positive, 
                    errorScale, 
                    errorTolerance, 
                    backSideSP, 
                    indexP2, 
                    out forceP2, 
                    out h_positive);
                //RungeKutta4(backSideSP.GetSubArray(indexP2 - 3, 3), RKDelta, backSideSP, indexP2, out forceP2);

                if (backSideSP[indexP2] > 0.5f || backSideSP[indexP2] < -0.5f
                || backSideSP[indexP2 + 1] > 0.5f || backSideSP[indexP2 + 1] < -0.5f
                || backSideSP[indexP2 + 2] > 0.5f || backSideSP[indexP2 + 2] < -0.5f)
                {
                    //Debug.Log("Positive out of range ");
                    P2Range = false;
                }
                // Check size of power and use P2Range variable
                else if (forceP2 < minimumForce)
                {
                    //Debug.Log("Positive force too small " + forceP2);
                    P2Range = false;
                }
                else
                {
                    indexP2 += 3;
                    P2Size++;
                }
                
            }else // P2Range & P1Range are false
            {
                break;
            }

            // DISTANCE / LOOP CHECK
            ptDistance = distance2(backSideSP.GetSubArray(indexP2 - 3, 3), streamlinePoints.GetSubArray(indexP1 - 3, 3));
            if (!checkForLoop && ptDistance < ptDistancePrevious)
                checkForLoop = true;
            if (checkForLoop && ptDistance > ptDistancePrevious)
            {
                if (ptDistance <= ClosureDelta * ClosureDelta) // we found a loop
                {
                    break;
                }
                else // points are drifting away again
                {
                    checkForLoop = false;
                }
            }
            ptDistancePrevious = ptDistance;
        }

        // CONCATENATE ARRAYS
        // line = a^-1 + b
        float temp;
        float temp2;
        float temp3;

        for (int i = 0; i < (indexP1) / 2; i+=3)
        {
            temp = streamlinePoints[i];
            temp2 = streamlinePoints[i + 1];
            temp3 = streamlinePoints[i + 2];
            streamlinePoints[i] = streamlinePoints[indexP1 - (i + 3)];
            streamlinePoints[i + 1] = streamlinePoints[indexP1 - (i + 2)];
            streamlinePoints[i + 2] = streamlinePoints[indexP1 - (i + 1)];
            streamlinePoints[indexP1 - (i + 3)] = temp;
            streamlinePoints[indexP1 - (i + 2)] = temp2;
            streamlinePoints[indexP1 - (i + 1)] = temp3;
        }
        // add b to the rest of the array
        for (int i = 3; i < indexP2; i+=3) // i = 3 because we dont need the seed point twice
        {
            streamlinePoints[indexP1 + i - 3] = backSideSP[i];
            streamlinePoints[indexP1 + i - 2] = backSideSP[i + 1];
            streamlinePoints[indexP1 + i - 1] = backSideSP[i + 2];
        }

        actualLinePoints[0] = P2Size + P1Size - 1;
    }

    public void Execute()
    {
        ReComputeStreamlinesCPU();
    }
}

public class Streamline : MonoBehaviour
{
    public bool isDeleted = false;
    public NativeArray<float> StreamlinePoints;
    public NativeArray<float> seedNative;
    public NativeArray<int> spaceDimNative;
    private NativeArray<float> backSideSP;
    public Vector3 Seed;
    public JobHandle handle;
    public NativeArray<int> actualLinePointCount;
    private int currentLinePoints;

    private ComputeShader generateTubes;
    private ComputeShader generateLine;

    public StreamlineGenerator ParentGenerator;

    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;

    private GraphicsBuffer vertexBuffer;
    private GraphicsBuffer indexBuffer;
    private GraphicsBuffer colorBuffer;
    private NativeArray<Vector3> vertexArray;
    private NativeArray<int> indexArray;
    private NativeArray<Vector4> colorArray;

    private NativeArray<float> con_k;
    private NativeArray<float> con_y;

    public bool DoAnimation;
    private float timeCounter;
    private float timeSpeed;
    public int arrowLength;    // Defines the arrow length in line points
    public int arrowGap;       // Defines the gap between arrows 
    private int maxVertices;
    private int arrowLengthCurrent;
    private int arrowGapCurrent;

    public void InitRKFConstants(NativeArray<float> k, NativeArray<float> y)
    {
        k[0] = 37.0f / 378.0f;
        k[1] = 250.0f / 621.0f;
        k[2] = 125.0f / 594.0f;
        k[3] = 512.0f / 1771.0f;
        k[4] = 2825.0f / 27648.0f;
        k[5] = 18575.0f / 48384.0f;
        k[6] = 13525.0f / 55296.0f;
        k[7] = 277.0f / 14336.0f;
        k[8] = 1.0f / 4.0f;

        y[0] = 1.0f / 5.0f;
        y[1] = 3.0f / 40.0f;
        y[2] = 9.0f / 40.0f;
        y[3] = 3.0f / 10.0f;
        y[4] = -9.0f / 10.0f;
        y[5] = 6.0f / 5.0f;
        y[6] = -11.0f / 54.0f;
        y[7] = 5.0f / 2.0f;
        y[8] = -70.0f / 27.0f;
        y[9] = 35.0f / 27.0f;
        y[10] = 1631.0f / 55296.0f;
        y[11] = 175.0f / 512.0f;
        y[12] = 575.0f / 13824.0f;
        y[13] = 44275.0f / 110592.0f;
        y[14] = 253.0f / 4096.0f;
    }

    public void Awake()
    {
        // We define the constants for RKF as soon as the object awakes
        con_k = new NativeArray<float>(9, Allocator.Persistent);
        con_y = new NativeArray<float>(15, Allocator.Persistent);

        InitRKFConstants(con_k, con_y);

    }

    /// <summary>
    /// Inits the streamline, especially the mesh
    /// </summary>
    /// <param name="pointCount">Maximum count of points within the curve</param>
    public void Init(int pointCount)
    {
        ParentGenerator = GetComponentInParent<StreamlineGenerator>();
        StreamlinePoints = new NativeArray<float>(pointCount * 3, Allocator.Persistent);
        actualLinePointCount = new NativeArray<int>(1, Allocator.Persistent); // we only need [0]
        //Debug.Log(actualLinePointCount[0]);
        seedNative = new NativeArray<float>(3, Allocator.Persistent);
        spaceDimNative = new NativeArray<int>(3, Allocator.Persistent);

        timeSpeed = 1.0f;

        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshFilter = gameObject.AddComponent<MeshFilter>();
        GenerateMesh(pointCount);
    }

    public void SetAnimationSpeed(float speed)
    {
        timeSpeed = speed;
    }

    public void GenerateMesh(int vertexCount)
    {
        Mesh mesh = new Mesh();
        mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        mesh.indexFormat = IndexFormat.UInt32;
        
        var layout = new[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4)
        };
        mesh.SetVertexBufferParams(vertexCount, layout);

        indexArray = new NativeArray<int>(vertexCount * 2, Allocator.Persistent);
        mesh.SetIndices(indexArray, MeshTopology.Lines, 0);
        vertexBuffer = mesh.GetVertexBuffer(0);
        indexBuffer = mesh.GetIndexBuffer();

        meshFilter.mesh = mesh;
    }

    public void UpdateMeshVertexIndexCount()
    {
        if (vertexArray != default)
        {
            vertexArray.Dispose();
            vertexArray = default;
        }
        if (indexArray != default)
        {
            indexArray.Dispose();
            indexArray = default;
        }
        vertexBuffer?.Dispose();
        indexBuffer?.Dispose();
        GenerateMesh(actualLinePointCount[0]);
        //Debug.Log("Indices and vertices updated");
    }

    public void SetAnimation(bool state)
    {
        DoAnimation = state;
    }

    public void FixedUpdate()
    {
        arrowLength = ParentGenerator.arrowLength;
        arrowGap = ParentGenerator.arrowGap;
        if (DoAnimation)
        {
            timeCounter += timeSpeed;
        }
    }

    public void Update()
    {
        
        
    }


    public void SetGenShader(ComputeShader genShader)
    {
        generateTubes = genShader;
    }

    public void SetLineShader(ComputeShader generateLineShader)
    {
        generateLine = generateLineShader;
    }

    public void SetMaterial(Material material)
    {
        meshRenderer.material = material;
    }


    /// <summary>
    /// This method is called once per loaded data set. Points are then used to create the mesh.
    /// </summary>
    /// <param name="dataset"></param>
    /// <param name="ClosureDelta"></param>
    /// <param name="RKDelta"></param>
    /// <param name="SpaceDimenstion"></param>
    /// <param name="sampleDim"></param>
    /// <param name="maxLinePoints"></param>
    public void CalculateStreamlinePoints(
        NativeArray<float> dataset,
        float ClosureDelta,
        float RKDelta,
        Vector3Int SpaceDimenstion,
        int sampleDim,
        int maxLinePoints
        )
    {
        seedNative[0] = Seed.x;
        seedNative[1] = Seed.y;
        seedNative[2] = Seed.z;

        spaceDimNative[0] = (int)SpaceDimenstion.x;
        spaceDimNative[1] = (int)SpaceDimenstion.y;
        spaceDimNative[2] = (int)SpaceDimenstion.z;

        backSideSP = new NativeArray<float>(maxLinePoints*3, Allocator.Persistent);

        CalcStreamlineJob jobData = new CalcStreamlineJob
        {
            dataset = dataset,
            seed = seedNative,
            ClosureDelta = ClosureDelta,
            RKDelta = RKDelta,
            SpaceDimension = spaceDimNative,
            sampleDim = sampleDim,
            streamlinePoints = StreamlinePoints,
            backSideSP = backSideSP,
            maxLinePoints = maxLinePoints,
            actualLinePoints = actualLinePointCount,
            con_k = this.con_k,
            con_y = this.con_y
        };

        Seed.x = seedNative[0];
        Seed.y = seedNative[1];
        Seed.z = seedNative[2];
        handle = jobData.Schedule();
    }

    public void UpdateArrowSize()
    {
        maxVertices = 0;
        int arrowCount = Mathf.FloorToInt(actualLinePointCount[0] / (arrowLength + arrowGap));

        if (arrowCount == 0)
        {
            arrowLength = Mathf.Min(arrowLength, actualLinePointCount[0]);
            arrowGap = actualLinePointCount[0] - arrowLength;
            
        }
        else
        {
            int resizedGap = Mathf.FloorToInt((actualLinePointCount[0] - arrowCount * arrowLength) / arrowCount);
            arrowGap = resizedGap;
        }

        
    }

    /// <summary>
    /// A line is generated from the previously calculated streamline points. This is done once per loaded data set or parameter set.
    /// Additional computations for each frame are done in separate compute/geometry/fragment shaders.
    /// </summary>
    public void GenerateMeshLine()
    {
        if (actualLinePointCount[0] != currentLinePoints)
        {
            UpdateMeshVertexIndexCount();
            currentLinePoints = actualLinePointCount[0];

        }

        ComputeBuffer inputBuffer = new ComputeBuffer(actualLinePointCount[0], sizeof(float) * 3);
        inputBuffer.SetData(StreamlinePoints, 0, 0, actualLinePointCount[0] * 3);

        generateLine.SetInt("arraySize", actualLinePointCount[0]);

        generateLine.SetBuffer(0, "InVertices", inputBuffer);
        generateLine.SetBuffer(0, "OutVertices", vertexBuffer);
        generateLine.SetBuffer(0, "OutIndices", indexBuffer);
        generateLine.Dispatch(0, Mathf.CeilToInt(actualLinePointCount[0] / 128.0f), 1, 1);
        
        inputBuffer.Release();
    }

    public void ProcessLine(StreamlineGenerator.Line mode)
    {
        if (isDeleted)
            return;
        if (arrowLengthCurrent != arrowLength || arrowGapCurrent != arrowGap)
        {
            UpdateArrowSize();
            arrowLengthCurrent = arrowLength;
            arrowGapCurrent = arrowGap;
        }

        //Debug.Log(actualLinePointCount[0]);

        generateTubes.SetInt("arraySize", actualLinePointCount[0]);
        generateTubes.SetInt("timeCounter", Mathf.RoundToInt(timeCounter));
        generateTubes.SetInt("arrowLength", arrowLength);
        generateTubes.SetInt("arrowGap", arrowGap);

        switch(mode)
        {
            case StreamlineGenerator.Line.ContinuosLine:
                generateTubes.SetBuffer(1, "OutVertices", vertexBuffer);
                generateTubes.SetBuffer(1, "OutIndices", indexBuffer);
                generateTubes.Dispatch(1, Mathf.CeilToInt(actualLinePointCount[0] / 128.0f), 1, 1);
                break;
            case StreamlineGenerator.Line.Arrows:
                generateTubes.SetBuffer(0, "OutVertices", vertexBuffer);
                generateTubes.SetBuffer(0, "OutIndices", indexBuffer);
                generateTubes.Dispatch(0, Mathf.CeilToInt(actualLinePointCount[0] / 128.0f), 1, 1);
                break;
            case StreamlineGenerator.Line.DashedLine:
                generateTubes.SetBuffer(2, "OutVertices", vertexBuffer);
                generateTubes.SetBuffer(2, "OutIndices", indexBuffer);
                generateTubes.Dispatch(2, Mathf.CeilToInt(actualLinePointCount[0] / 128.0f), 1, 1);
                break;
            default:
                break;
        }

    }

    public void GenerateTubes(StreamlineGenerator.Line mode)
    { 

        if (actualLinePointCount[0] != currentLinePoints)
        {
            UpdateMeshVertexIndexCount();
            currentLinePoints = actualLinePointCount[0];
            
        }

        if (arrowLengthCurrent != arrowLength || arrowGapCurrent != arrowGap)
        {
            UpdateArrowSize();
            arrowLengthCurrent = arrowLength;
            arrowGapCurrent = arrowGap;
        }

        ComputeBuffer inputBuffer = new ComputeBuffer(actualLinePointCount[0], sizeof(float) * 3);
        inputBuffer.SetData(StreamlinePoints, 0, 0, actualLinePointCount[0] * 3);

        generateTubes.SetInt("vectorSize", actualLinePointCount[0]);
        generateTubes.SetInt("timeCounter", Mathf.RoundToInt(timeCounter));
        generateTubes.SetInt("maxVertices", maxVertices);
        generateTubes.SetInt("arrowLength", arrowLength);
        generateTubes.SetInt("arrowGap", arrowGap);

        switch (mode)
        {
            case StreamlineGenerator.Line.DashedLine:
                generateTubes.SetBuffer(0, "InVertices", inputBuffer);
                generateTubes.SetBuffer(0, "OutVertices", vertexBuffer);
                generateTubes.SetBuffer(0, "OutIndices", indexBuffer);
                generateTubes.Dispatch(0, Mathf.CeilToInt(actualLinePointCount[0] / 128.0f), 1, 1);
                break;
            case StreamlineGenerator.Line.ContinuosLine:
                generateTubes.SetBuffer(1, "InVertices", inputBuffer);
                generateTubes.SetBuffer(1, "OutVertices", vertexBuffer);
                generateTubes.SetBuffer(1, "OutIndices", indexBuffer);
                generateTubes.Dispatch(1, Mathf.CeilToInt(actualLinePointCount[0] / 128.0f), 1, 1);
                break;
            case StreamlineGenerator.Line.Arrows:
                break;
            default:
                generateTubes.Dispatch(0, Mathf.CeilToInt(actualLinePointCount[0] / 128.0f), 1, 1);
                break;
        }

        inputBuffer.Release();
        
    }

    public void CleanAfterCalculation()
    {
        if (backSideSP != default)
        {
            backSideSP.Dispose();
            backSideSP = default;
        }
    }

    public void CleanUp()
    {
        if (StreamlinePoints != default)
        {
            StreamlinePoints.Dispose();
            StreamlinePoints = default;
        }
        if (actualLinePointCount != default)
        {
            actualLinePointCount.Dispose();
            actualLinePointCount = default;
        }
        if (spaceDimNative != default)
        {
            spaceDimNative.Dispose();
            spaceDimNative = default;
        }
        if (seedNative != default)
        {
            seedNative.Dispose();
            seedNative = default;
        }

        if (vertexArray != default)
        {
            vertexArray.Dispose();
            vertexArray = default;
        }
        if (indexArray != default)
        {
            indexArray.Dispose();
            indexArray = default;
        }
        if (backSideSP != default)
        {
            backSideSP.Dispose();
            backSideSP = default;
        }
        if (con_k != default)
        {
            con_k.Dispose();
            con_k = default;
        }
        if (con_y != default)
        {
            con_y.Dispose();
            con_y = default;
        }

        indexBuffer?.Dispose();
        vertexBuffer?.Dispose();
    }

    public void OnApplicationQuit() // Cleans up when application is quitted
    {
        CleanUp();
    }

    public void OnDestroy()
    {
        CleanUp();
    }


}
