/*
  Createdy by Juan Sebastian Munoz Arango
  naruse@gmail.com
  All rights reserved
 */

namespace ProDrawCall {
    using UnityEngine;
    using UnityEditor;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;

    public sealed class SelectNonMalformedUVs {
        static readonly SelectNonMalformedUVs instance = new SelectNonMalformedUVs();//SelectNonMalformedUVs();
        public static SelectNonMalformedUVs Instance { get { return instance; } }

        private SelectNonMalformedUVs() {}

        public class ThreadData {
		    private int start;
            public int Start { get { return start; } }
            private int end;
		    public int End { get { return end; } }
			public ThreadData (int s, int e) {
				start = s;
				end = e;
			}
		}

        private int threadsFinishedCount = 0;
        private Vector2 minUV = new Vector2(0,0);
        private Vector2 maxUV = new Vector2(1,1);

        //as threads cant access anything from Unity, meshes UVs have to be
        //extracted from main thread. this list has the same length than rawSelectedObjects
        private List<Vector2[]> extractedMeshesUVs;
        private List<GameObject> correctNonMalformedObjects;
        private GameObject[] rawSelectedObjects;//contains the unfiltered objects to process (Same length than extractedMeshesUVs)
        private Mutex mutex;

        //Entry point
        public void ThreadedSelectNonMalformedUvsObjs(Vector2 _minUV, Vector2 _maxUV, GameObject[] currentSelectedObjects = null) {
            rawSelectedObjects = (currentSelectedObjects == null) ? Utils.GetAllObjectsInHierarchy() : currentSelectedObjects;
            if(rawSelectedObjects.Length == 0)
                return;

            minUV = _minUV;
            maxUV = _maxUV;

            if(correctNonMalformedObjects == null)
                correctNonMalformedObjects = new List<GameObject>();
            correctNonMalformedObjects.Clear();

            if(extractedMeshesUVs == null)
                extractedMeshesUVs = new List<Vector2[]>();
            extractedMeshesUVs.Clear();

            for(int i = 0; i < rawSelectedObjects.Length; i++)
                extractedMeshesUVs.Add(ExtractObjMainUVs(rawSelectedObjects[i]));

            int cores = Mathf.Min(SystemInfo.processorCount, rawSelectedObjects.Length);
            int slice = rawSelectedObjects.Length/cores;

            threadsFinishedCount = 0;
            if(mutex == null)
                mutex = new Mutex(false);
            if(cores > 1) {
                ThreadData threadData;
                int i = 0;
                for(i = 0; i < cores-1; i++) {
                    threadData = new ThreadData(slice * i, slice * (i+1));
                    ParameterizedThreadStart ts = new ParameterizedThreadStart(CheckGameObjects);
                    Thread thread = new Thread(ts);
                    thread.Start(threadData);
                }
                threadData = new ThreadData(slice*i, rawSelectedObjects.Length);
                CheckGameObjects(threadData);
                while(threadsFinishedCount < cores)
                    Thread.Sleep(1);//main thread waits all threads finish
            } else {
                ThreadData threadData = new ThreadData(0, rawSelectedObjects.Length);
                CheckGameObjects(threadData);
            }
            Selection.objects = correctNonMalformedObjects.ToArray();
        }

        private void CheckGameObjects(System.Object obj) {
            ThreadData threadData = (ThreadData) obj;
            for(int i = threadData.Start; i < threadData.End; i++)
                CheckAndAddCorrectUVGameObj(extractedMeshesUVs[i], i);

            mutex.WaitOne();
            threadsFinishedCount++;
            mutex.ReleaseMutex();
        }

        // Checks uv correctness on a game object and adds it to the list
        // of correctly UV objects if its correctly assembled
        private void CheckAndAddCorrectUVGameObj(Vector2[] objMeshUVs, int indexObjMesh) {
            bool objMalformed = objMeshUVs == null;

            //TODO: Make it work with UV2,3,4
            if(!objMalformed)
                for(int i = 0; i < objMeshUVs.Length; i++) {
                    Vector2 currUV = objMeshUVs[i];
                    if(currUV.x < minUV.x || currUV.x > maxUV.x || currUV.y < minUV.y || currUV.y > maxUV.y) {
                        objMalformed = true;
                        break;
                    }
                }
            mutex.WaitOne();
            if(!objMalformed)
                correctNonMalformedObjects.Add(rawSelectedObjects[indexObjMesh]);
            mutex.ReleaseMutex();
        }

        private Vector2[] ExtractObjMainUVs(GameObject g) {
            if(g.GetComponent<MeshFilter>())
                return g.GetComponent<MeshFilter>().sharedMesh.uv;
            else if(g.GetComponent<SkinnedMeshRenderer>())
                return g.GetComponent<SkinnedMeshRenderer>().sharedMesh.uv;
            else
                return null;
        }
    }
}