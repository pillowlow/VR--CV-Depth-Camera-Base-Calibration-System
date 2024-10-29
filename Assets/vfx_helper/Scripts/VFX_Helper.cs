
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.VFX;


public class VFX_Helper : MonoBehaviour
{
    public List<VFXTargetComponent> targetComponents;
    public List<VFXTransition> transitions;
    private bool isTransitioning = false; // Flag to track if a transition is in progress

    // set state directly
    public void SetState(string stateName, float waitSeconds = 0f)
    {
        // Find the VFX state by name
        VFXTransition targetState = transitions.Find(t => t.name == stateName);

        if (targetState == null)
        {
            Debug.LogWarning($"State '{stateName}' not found.");
            return;
        }

        // If there's a wait time, start a coroutine to wait before applying the state
        if (waitSeconds > 0f)
        {
            StartCoroutine(ApplyStateAfterDelay(targetState, waitSeconds));
        }
        else
        {
            // Apply the state immediately
            ApplyState(targetState);
        }
    }

    private IEnumerator ApplyStateAfterDelay(VFXTransition targetState, float waitSeconds)
    {
        yield return new WaitForSeconds(waitSeconds);
        ApplyState(targetState);
    }
    // Apply the target state immediately
    private void ApplyState(VFXTransition targetState)
    {
        // Traverse the target components
        foreach (var target in targetComponents)
        {
            // Handle Renderer & Material Pairs
            foreach (var rendererMaterialsPair in target.rendererMaterialsPairs)
            {
                Renderer renderer = rendererMaterialsPair.targetRenderer;
                List<Material> materials = rendererMaterialsPair.targetMaterials;

                foreach (var material in materials)
                {
                    // Apply float parameters
                    foreach (var floatParam in targetState.toState.materialState.FloatSets)
                    {
                        material.SetFloat(floatParam.para_name, floatParam.float_para);
                    }

                    // Apply color parameters
                    foreach (var colorParam in targetState.toState.materialState.ColorSets)
                    {
                        material.SetColor(colorParam.para_name, colorParam.color_para);
                    }

                    // Apply bool parameters
                    foreach (var boolParam in targetState.toState.materialState.BoolSets)
                    {
                        material.SetFloat(boolParam.para_name, boolParam.bool_para ? 1f : 0f);
                    }
                }
            }

            // Handle Light Components
            foreach (var light in target.targetLights)
            {
                light.color = targetState.toState.lightState.color;
                light.intensity = targetState.toState.lightState.intensity;
                light.range = targetState.toState.lightState.range;
            }

            // Handle Transform Components
            foreach (var targetTransform in target.targetTransforms)
            {
                Vector3 targetPosition= targetState.toState.transformState.position;
                Vector3 targetRotation= targetState.toState.transformState.rotation;
                Vector3 targetScale= targetState.toState.transformState.scale;
                if (targetState.toState.transformState.mode == CoordinateMode.Local)
                {
                    targetTransform.localPosition = targetPosition;
                    targetTransform.localEulerAngles = targetRotation;
                    targetTransform.localScale = targetScale;
                }
                else
                {
                    targetTransform.position =  targetPosition;
                    targetTransform.rotation = Quaternion.Euler(targetRotation);
                    targetTransform.localScale = targetScale;
                }
            }

            // Handle VFX Graph Components
            foreach (var vfxGraph in target.targetVFXGraphs)
            {
                foreach (var floatParam in targetState.toState.vfxGraphState.FloatSets)
                {
                    vfxGraph.SetFloat(floatParam.para_name, floatParam.float_para);
                }

                foreach (var colorParam in targetState.toState.vfxGraphState.ColorSets)
                {
                    vfxGraph.SetVector4(colorParam.para_name, colorParam.color_para);
                }

                foreach (var intParam in targetState.toState.vfxGraphState.IntSets)
                {
                    vfxGraph.SetInt(intParam.para_name, intParam.int_para);
                }

                foreach (var vector3Param in targetState.toState.vfxGraphState.Vector3Sets)
                {
                    vfxGraph.SetVector3(vector3Param.para_name, vector3Param.vector3_para);
                }

                foreach (var boolParam in targetState.toState.vfxGraphState.BoolSets)
                {
                    vfxGraph.SetBool(boolParam.para_name, boolParam.bool_para);
                }
            }

            // Handle Particle System Components
            foreach (var particleSystem in target.targetParticleSystems)
            {
                var mainModule = particleSystem.main;
                var emissionModule = particleSystem.emission;

                foreach (var floatParam in targetState.toState.particleSystemState.FloatSets)
                {
                    switch (floatParam.para_name)
                    {
                        case "startLifetime":
                            mainModule.startLifetime = floatParam.float_para;
                            break;
                        case "startSpeed":
                            mainModule.startSpeed = floatParam.float_para;
                            break;
                        case "startSize":
                            mainModule.startSize = floatParam.float_para;
                            break;
                        case "gravityModifier":
                            mainModule.gravityModifier = floatParam.float_para;
                            break;
                        case "emissionRateOverTime":
                            emissionModule.rateOverTime = floatParam.float_para;
                            break;
                    }
                }

                foreach (var colorParam in targetState.toState.particleSystemState.ColorSets)
                {
                    if (colorParam.para_name == "startColor")
                    {
                        mainModule.startColor = colorParam.color_para;
                    }
                }

                foreach (var vector3Param in targetState.toState.particleSystemState.Vector3Sets)
                {
                    switch (vector3Param.para_name)
                    {
                        case "startSize3D":
                            mainModule.startSizeXMultiplier = vector3Param.vector3_para.x;
                            mainModule.startSizeYMultiplier = vector3Param.vector3_para.y;
                            mainModule.startSizeZMultiplier = vector3Param.vector3_para.z;
                            break;
                        case "startRotation3D":
                            mainModule.startRotationXMultiplier = vector3Param.vector3_para.x;
                            mainModule.startRotationYMultiplier = vector3Param.vector3_para.y;
                            mainModule.startRotationZMultiplier = vector3Param.vector3_para.z;
                            break;
                    }
                }

                foreach (var boolParam in targetState.toState.particleSystemState.BoolSets)
                {
                    if (boolParam.para_name == "emissionEnabled")
                    {
                        emissionModule.enabled = boolParam.bool_para;
                    }
                }
            }
        }

        Debug.Log($"State '{targetState.name}' applied immediately.");
    }

    // Method to trigger a transition by name
    public void PlayTransition(string transitionName)
    {   
    
        if (isTransitioning)
        {
            Debug.LogWarning($"Transition request for '{transitionName}' blocked because another transition is already in progress.");
            return; // Block the request if a transition is already running
        }

        VFXTransition transition = transitions.Find(t => t.name == transitionName);
        if (transition != null)
        {   
            Debug.Log($"start transition '{transitionName}' ");
            List<ProcessingItem> processingQueue = BuildProcessingQueue(transition);
            StartCoroutine(PerformTransition(processingQueue, transition));
        }
        else
        {
            Debug.LogWarning($"Transition with name {transitionName} not found!");
        }
    }

    // Method to build the processing queue
    private List<ProcessingItem> BuildProcessingQueue(VFXTransition transition)
    {
        List<ProcessingItem> processingQueue = new List<ProcessingItem>();

        // Traverse the target components
        foreach (var target in targetComponents)
        {
            // Iterate through each RendererMaterialsPair
            foreach (var rendererMaterialsPair in target.rendererMaterialsPairs)
            {
                Renderer renderer = rendererMaterialsPair.targetRenderer;
                List<Material> materials = rendererMaterialsPair.targetMaterials;

                //Debug.Log($"Processing Renderer: {renderer.name}, Materials Count: {materials.Count}");

                // Iterate through each material in the pair
                foreach (var material in materials)
                {
                    //Debug.Log($"Processing Material: {material.name}");

                    if (transition.fromState != null && transition.toState != null)
                    {
                        // Traverse through all float parameters in fromState and toState
                        foreach (var fromFloat in transition.fromState.materialState.FloatSets)
                        {
                            foreach (var toFloat in transition.toState.materialState.FloatSets)
                            {
                                if (fromFloat.para_name == toFloat.para_name)
                                {
                                    //Debug.Log($"Adding Float Parameter: {fromFloat.para_name} to Queue for Material: {material.name}");
                                    processingQueue.Add(new ProcessingItem(fromFloat.para_name, ParameterType.Float, renderer, material));
                                }
                            }
                        }

                        // Traverse through all color parameters in fromState and toState
                        foreach (var fromColor in transition.fromState.materialState.ColorSets)
                        {
                            foreach (var toColor in transition.toState.materialState.ColorSets)
                            {
                                if (fromColor.para_name == toColor.para_name)
                                {
                                    //Debug.Log($"Adding Color Parameter: {fromColor.para_name} to Queue for Material: {material.name}");
                                    processingQueue.Add(new ProcessingItem(fromColor.para_name, ParameterType.Color, renderer, material));
                                }
                            }
                        }

                        // Traverse through all bool parameters in fromState and toState
                        foreach (var fromBool in transition.fromState.materialState.BoolSets)
                        {
                            foreach (var toBool in transition.toState.materialState.BoolSets)
                            {
                                if (fromBool.para_name == toBool.para_name)
                                {
                                    //Debug.Log($"Adding Bool Parameter: {fromBool.para_name} to Queue for Material: {material.name}");
                                    processingQueue.Add(new ProcessingItem(fromBool.para_name, ParameterType.Bool, renderer, material));
                                }
                            }
                        }
                    }
                }
            }
            // Handle Light Components
           
            foreach (var light in target.targetLights)
            {
                if (light != null && transition.fromState != null && transition.toState != null)
                {
                    // Process intensity (float)
                    processingQueue.Add(new ProcessingItem("intensity", ParameterType.Float, light));

                    // Process color (Color)
                    processingQueue.Add(new ProcessingItem("color", ParameterType.Color, light));

                    // Process range (float)
                    processingQueue.Add(new ProcessingItem("range", ParameterType.Float, light));
                }
            }


            // Handle Transform Components
            foreach (var transformState in target.targetTransforms)
            {
                if (transformState != null && transition.fromState != null && transition.toState != null)
                {
                    // Add position, rotation, and scale to the processing queue
                    processingQueue.Add(new ProcessingItem("position", ParameterType.Vector3, transformState.transform));
                    processingQueue.Add(new ProcessingItem("rotation", ParameterType.Vector3, transformState.transform));
                    processingQueue.Add(new ProcessingItem("scale", ParameterType.Vector3, transformState.transform));
                }
            }

            // Handle VFX Graph Components
            foreach (var vfxGraph in target.targetVFXGraphs)
            {
                if (vfxGraph != null && transition.fromState != null && transition.toState != null)
                {
                    // Traverse through all float parameters in fromState and toState
                    foreach (var fromFloat in transition.fromState.vfxGraphState.FloatSets)
                    {
                        foreach (var toFloat in transition.toState.vfxGraphState.FloatSets)
                        {
                            if (fromFloat.para_name == toFloat.para_name)
                            {
                                processingQueue.Add(new ProcessingItem(fromFloat.para_name, ParameterType.Float, vfxGraph));
                            }
                        }
                    }

                    // Traverse through all color parameters in fromState and toState
                    foreach (var fromColor in transition.fromState.vfxGraphState.ColorSets)
                    {
                        foreach (var toColor in transition.toState.vfxGraphState.ColorSets)
                        {
                            if (fromColor.para_name == toColor.para_name)
                            {
                                processingQueue.Add(new ProcessingItem(fromColor.para_name, ParameterType.Color, vfxGraph));
                            }
                        }
                    }

                    // Traverse through all int parameters in fromState and toState
                    foreach (var fromInt in transition.fromState.vfxGraphState.IntSets)
                    {
                        foreach (var toInt in transition.toState.vfxGraphState.IntSets)
                        {
                            if (fromInt.para_name == toInt.para_name)
                            {
                                processingQueue.Add(new ProcessingItem(fromInt.para_name, ParameterType.Int, vfxGraph));
                            }
                        }
                    }

                    // Traverse through all vector3 parameters in fromState and toState
                    foreach (var fromVector3 in transition.fromState.vfxGraphState.Vector3Sets)
                    {
                        foreach (var toVector3 in transition.toState.vfxGraphState.Vector3Sets)
                        {
                            if (fromVector3.para_name == toVector3.para_name)
                            {
                                processingQueue.Add(new ProcessingItem(fromVector3.para_name, ParameterType.Vector3, vfxGraph));
                            }
                        }
                    }

                    // Traverse through all bool parameters in fromState and toState
                    foreach (var fromBool in transition.fromState.vfxGraphState.BoolSets)
                    {
                        foreach (var toBool in transition.toState.vfxGraphState.BoolSets)
                        {
                            if (fromBool.para_name == toBool.para_name)
                            {
                                processingQueue.Add(new ProcessingItem(fromBool.para_name, ParameterType.Bool, vfxGraph));
                            }
                        }
                    }
                }
            }


            foreach (var particleSystem in target.targetParticleSystems)
            {
                if (particleSystem != null && transition.fromState != null && transition.toState != null)
                {
                    // Traverse through all float parameters in fromState and toState
                    foreach (var fromFloat in transition.fromState.particleSystemState.FloatSets)
                    {
                        foreach (var toFloat in transition.toState.particleSystemState.FloatSets)
                        {
                            if (fromFloat.para_name == toFloat.para_name)
                            {
                                processingQueue.Add(new ProcessingItem(fromFloat.para_name, ParameterType.Float, particleSystem));
                            }
                        }
                    }

                    // Traverse through all color parameters in fromState and toState
                    foreach (var fromColor in transition.fromState.particleSystemState.ColorSets)
                    {
                        foreach (var toColor in transition.toState.particleSystemState.ColorSets)
                        {
                            if (fromColor.para_name == toColor.para_name)
                            {
                                processingQueue.Add(new ProcessingItem(fromColor.para_name, ParameterType.Color, particleSystem));
                            }
                        }
                    }


                    // Traverse through all bool parameters in fromState and toState
                    foreach (var fromBool in transition.fromState.particleSystemState.BoolSets)
                    {
                        foreach (var toBool in transition.toState.particleSystemState.BoolSets)
                        {
                            if (fromBool.para_name == toBool.para_name)
                            {
                                processingQueue.Add(new ProcessingItem(fromBool.para_name, ParameterType.Bool, particleSystem));
                            }
                        }
                    }

                    // Traverse through all vector3 parameters in fromState and toState
                    foreach (var fromVector3 in transition.fromState.particleSystemState.Vector3Sets)
                    {
                        foreach (var toVector3 in transition.toState.particleSystemState.Vector3Sets)
                        {
                            if (fromVector3.para_name == toVector3.para_name)
                            {
                                processingQueue.Add(new ProcessingItem(fromVector3.para_name, ParameterType.Vector3, particleSystem));
                            }
                        }
                    }
                }
            }
            
        }

        Debug.Log($"Total Processing Items in Queue: {processingQueue.Count}");
        return processingQueue;
    }


    
    // Coroutine to perform the transition over time
    private IEnumerator PerformTransition(List<ProcessingItem> processingQueue, VFXTransition transition)
    {
        //Debug.Log("PerformTransition started."); // Log when the transition starts

        isTransitioning = true; // Set the flag to indicate a transition is in progress
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch(); // Stopwatch to measure actual time
        stopwatch.Start();

        // Log the number of items in the processing queue
        //Debug.Log($"Processing Queue Count: {processingQueue.Count}");

        // Start processing coroutines for different types of items
        List<Coroutine> processingCoroutines = new List<Coroutine>();

        foreach (var item in processingQueue)
        {
            //Debug.Log($"Processing item: {item.parameterName} using Component: {item.targetComponent?.GetType().Name}");

            if (item.targetMaterial != null && item.targetComponent is Renderer)
            {
                processingCoroutines.Add(StartCoroutine(ProcessMaterialItem(item, transition)));
            }
            else if (item.targetComponent is Light)
            {
                processingCoroutines.Add(StartCoroutine(ProcessLightItem(item, transition)));
            }
            else if (item.targetComponent is Transform)
            {
                processingCoroutines.Add(StartCoroutine(ProcessTransformItem(item, transition)));
            }
            else if (item.targetComponent is VisualEffect)
            {
                processingCoroutines.Add(StartCoroutine(ProcessVFXGraphItem(item, transition)));
            }
            else if (item.targetComponent is ParticleSystem)
            {
                processingCoroutines.Add(StartCoroutine(ProcessParticleSystemItem(item, transition)));
            }
        }

        // Wait until all coroutines are finished
        //Debug.Log("Waiting for all processing coroutines to complete...");
        foreach (var coroutine in processingCoroutines)
        {
            yield return coroutine;
        }

        stopwatch.Stop();
        Debug.Log($"VFX Transition Complete. Actual time spent: {stopwatch.Elapsed.TotalSeconds} seconds. Expected duration: {transition.processingStateAsset.processingState.duration} seconds.");

        isTransitioning = false; // Reset the flag once the transition is complete
        Debug.Log("isTransitioning flag reset. Transition process complete.");
    }

    public bool CheckIsTransitioning(){
        return isTransitioning;
    }


    // processing each kinds

    private IEnumerator ProcessMaterialItem(ProcessingItem item, VFXTransition transition)
    {
        float elapsedTime = 0f;
        VFXProcessingState processingState = transition.processingStateAsset.processingState;

       // Debug.Log($"Checking duration for transition. Duration: {processingState.duration} seconds");

        Renderer targetRenderer = item.targetComponent as Renderer;

        if (processingState.duration <= 0)
        {
            Debug.LogError($"Invalid duration: {processingState.duration}. Transition cannot proceed.");
            yield break;
        }

        if (targetRenderer != null)
        {
            Material material = item.targetMaterial;

            //Debug.Log($"Item Target Material: {material.name} (Instance ID: {material.GetInstanceID()})");

            // Log all shared materials in the renderer
            //Debug.Log($"Shared Materials in {targetRenderer.name}:");
            foreach (var sharedMat in targetRenderer.sharedMaterials)
            {
                //Debug.Log($" - {sharedMat.name} (Instance ID: {sharedMat.GetInstanceID()})");
            }

            if (material != null && System.Array.Exists(targetRenderer.sharedMaterials, sharedMat => sharedMat == material))
            {
                //Debug.Log($"Starting processing on material: {material.name} for parameter: {item.parameterName}");

                while (elapsedTime < processingState.duration)
                {
                    elapsedTime += Time.deltaTime;
                    float normalizedTime = Mathf.Clamp01(elapsedTime / processingState.duration);
                    float curveValue = processingState.transitionCurve.Evaluate(normalizedTime);

                    //Debug.Log($"Processing {item.parameterName} on {material.name}. Elapsed Time: {elapsedTime}, Normalized Time: {normalizedTime}, Curve Value: {curveValue}");

                    if (item.parameterType == ParameterType.Float)
                    {
                        float fromValue = transition.fromState.materialState.FloatSets.Find(p => p.para_name == item.parameterName).float_para;
                        float toValue = transition.toState.materialState.FloatSets.Find(p => p.para_name == item.parameterName).float_para;
                        float newValue = Mathf.Lerp(fromValue, toValue, curveValue);
                       // Debug.Log($"Setting float parameter '{item.parameterName}' from {fromValue} to {newValue}");
                        material.SetFloat(item.parameterName, newValue);
                    }
                    else if (item.parameterType == ParameterType.Color)
                    {
                        Color fromValue = transition.fromState.materialState.ColorSets.Find(p => p.para_name == item.parameterName).color_para;
                        Color toValue = transition.toState.materialState.ColorSets.Find(p => p.para_name == item.parameterName).color_para;
                        Color newValue = Color.Lerp(fromValue, toValue, curveValue);
                        //Debug.Log($"Setting color parameter '{item.parameterName}' from {fromValue} to {newValue}");
                        material.SetColor(item.parameterName, newValue);
                    }
                    else if (item.parameterType == ParameterType.Bool)
                    {
                        bool fromValue = transition.fromState.materialState.BoolSets.Find(p => p.para_name == item.parameterName).bool_para;
                        bool toValue = transition.toState.materialState.BoolSets.Find(p => p.para_name == item.parameterName).bool_para;
                        bool newValue = curveValue > 0.5f ? toValue : fromValue;
                        //Debug.Log($"Setting bool parameter '{item.parameterName}' to {newValue}");
                        material.SetFloat(item.parameterName, newValue ? 1.0f : 0.0f);
                    }

                    yield return null; // Continue on the next frame
                }

                //Debug.Log($"Finished processing on material: {material.name} for parameter: {item.parameterName}");
            }
            else
            {
                Debug.LogWarning($"Material {material.name} not found in renderer {targetRenderer.name}");
            }
        }
        else
        {
            Debug.LogWarning($"Target component is not a Renderer or is null for parameter: {item.parameterName}");
        }
    }






    private IEnumerator ProcessLightItem(ProcessingItem item, VFXTransition transition)
    {
        float elapsedTime = 0f;
        VFXProcessingState processingState = transition.processingStateAsset.processingState;

        Light targetLight = item.targetComponent as Light;

        if (targetLight != null)
        {
            //Debug.Log($"Starting processing on Light for parameter: {item.parameterName}");

            while (elapsedTime < processingState.duration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / processingState.duration);
                float curveValue = processingState.transitionCurve.Evaluate(normalizedTime);

                if (item.parameterType == ParameterType.Float && item.parameterName == "intensity")
                {
                    float fromValue = transition.fromState.lightState.intensity;
                    float toValue = transition.toState.lightState.intensity;
                    float newValue = Mathf.Lerp(fromValue, toValue, curveValue);
                    //Debug.Log($"Setting light intensity from {fromValue} to {newValue}");
                    targetLight.intensity = newValue;
                }
                else if (item.parameterType == ParameterType.Color && item.parameterName == "color")
                {
                    Color fromValue = transition.fromState.lightState.color;
                    Color toValue = transition.toState.lightState.color;
                    Color newValue = Color.Lerp(fromValue, toValue, curveValue);
                    //Debug.Log($"Setting light color from {fromValue} to {newValue}");
                    targetLight.color = newValue;
                }
                else if (item.parameterType == ParameterType.Float && item.parameterName == "range")
                {
                    float fromValue = transition.fromState.lightState.range;
                    float toValue = transition.toState.lightState.range;
                    float newValue = Mathf.Lerp(fromValue, toValue, curveValue);
                    //Debug.Log($"Setting light range from {fromValue} to {newValue}");
                    targetLight.range = newValue;
                }

                yield return null;
            }

            //Debug.Log($"Finished processing on Light for parameter: {item.parameterName}");
        }
    }




    private IEnumerator ProcessTransformItem(ProcessingItem item, VFXTransition transition)
    {
        float elapsedTime = 0f;
        VFXProcessingState processingState = transition.processingStateAsset.processingState;

        Transform targetTransform = item.targetComponent as Transform;

        if (targetTransform != null)
        {
            //Debug.Log($"Starting processing on Transform for parameter: {item.parameterName}");

            TransformState fromState = transition.fromState.transformState;
            TransformState toState = transition.toState.transformState;

            while (elapsedTime < processingState.duration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / processingState.duration);
                float curveValue = processingState.transitionCurve.Evaluate(normalizedTime);

                if (item.parameterType == ParameterType.Vector3)
                {
                    if (item.parameterName == "position")
                    {
                        Vector3 newValue = Vector3.Lerp(fromState.position, toState.position, curveValue);
                        //Debug.Log($"Setting transform position to {newValue}");

                        if (toState.mode == CoordinateMode.Local)
                        {
                            targetTransform.localPosition = newValue;
                        }
                        else
                        {
                            targetTransform.position = newValue;
                        }
                    }
                    else if (item.parameterName == "rotation")
                    {
                        Quaternion fromValue = Quaternion.Euler(fromState.rotation);
                        Quaternion toValue = Quaternion.Euler(toState.rotation);
                        Quaternion newValue = Quaternion.Lerp(fromValue, toValue, curveValue);
                        //Debug.Log($"Setting transform rotation to {newValue.eulerAngles}");

                        if (toState.mode == CoordinateMode.Local)
                        {
                            targetTransform.localRotation = newValue;
                        }
                        else
                        {
                            targetTransform.rotation = newValue;
                        }
                    }
                    else if (item.parameterName == "scale")
                    {
                        Vector3 newValue = Vector3.Lerp(fromState.scale, toState.scale, curveValue);
                        //Debug.Log($"Setting transform scale to {newValue}");
                        targetTransform.localScale = newValue;
                    }
                }

                yield return null; // Continue on the next frame
            }

            //Debug.Log($"Finished processing on Transform for parameter: {item.parameterName}");
        }
    }

    private IEnumerator ProcessVFXGraphItem(ProcessingItem item, VFXTransition transition)
    {
        float elapsedTime = 0f;
        VFXProcessingState processingState = transition.processingStateAsset.processingState;

        VisualEffect targetVFX = item.targetComponent as VisualEffect;

        if (targetVFX != null)
        {
            Debug.Log($"Starting processing on VFX Graph for parameter: {item.parameterName}");

            while (elapsedTime < processingState.duration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / processingState.duration);
                float curveValue = processingState.transitionCurve.Evaluate(normalizedTime);

                if (item.parameterType == ParameterType.Float)
                {
                    float fromValue = transition.fromState.vfxGraphState.FloatSets.Find(p => p.para_name == item.parameterName).float_para;
                    float toValue = transition.toState.vfxGraphState.FloatSets.Find(p => p.para_name == item.parameterName).float_para;
                    float newValue = Mathf.Lerp(fromValue, toValue, curveValue);
                    //Debug.Log($"Setting float parameter '{item.parameterName}' from {fromValue} to {newValue}");
                    targetVFX.SetFloat(item.parameterName, newValue);
                }
                else if (item.parameterType == ParameterType.Color)
                {
                    Color fromValue = transition.fromState.vfxGraphState.ColorSets.Find(p => p.para_name == item.parameterName).color_para;
                    Color toValue = transition.toState.vfxGraphState.ColorSets.Find(p => p.para_name == item.parameterName).color_para;
                    Color newValue = Color.Lerp(fromValue, toValue, curveValue);
                    //Debug.Log($"Setting color parameter '{item.parameterName}' from {fromValue} to {newValue}");
                    targetVFX.SetVector4(item.parameterName, newValue);
                }
                else if (item.parameterType == ParameterType.Int)
                {
                    int fromValue = transition.fromState.vfxGraphState.IntSets.Find(p => p.para_name == item.parameterName).int_para;
                    int toValue = transition.toState.vfxGraphState.IntSets.Find(p => p.para_name == item.parameterName).int_para;
                    int newValue = Mathf.RoundToInt(Mathf.Lerp(fromValue, toValue, curveValue));
                    //Debug.Log($"Setting int parameter '{item.parameterName}' from {fromValue} to {newValue}");
                    targetVFX.SetInt(item.parameterName, newValue);
                }
                else if (item.parameterType == ParameterType.Vector3)
                {
                    Vector3 fromValue = transition.fromState.vfxGraphState.Vector3Sets.Find(p => p.para_name == item.parameterName).vector3_para;
                    Vector3 toValue = transition.toState.vfxGraphState.Vector3Sets.Find(p => p.para_name == item.parameterName).vector3_para;
                    Vector3 newValue = Vector3.Lerp(fromValue, toValue, curveValue);
                    //Debug.Log($"Setting vector3 parameter '{item.parameterName}' from {fromValue} to {newValue}");
                    targetVFX.SetVector3(item.parameterName, newValue);
                }
                else if (item.parameterType == ParameterType.Bool)
                {
                    bool fromValue = transition.fromState.vfxGraphState.BoolSets.Find(p => p.para_name == item.parameterName).bool_para;
                    bool toValue = transition.toState.vfxGraphState.BoolSets.Find(p => p.para_name == item.parameterName).bool_para;
                    bool newValue = curveValue > 0.5f ? toValue : fromValue;
                    //Debug.Log($"Setting bool parameter '{item.parameterName}' to {newValue}");
                    targetVFX.SetBool(item.parameterName, newValue);
                }

                yield return null; // Continue on the next frame
            }

            Debug.Log($"Finished processing on VFX Graph for parameter: {item.parameterName}");
        }
    }


    private IEnumerator ProcessParticleSystemItem(ProcessingItem item, VFXTransition transition)
    {
        float elapsedTime = 0f;
        VFXProcessingState processingState = transition.processingStateAsset.processingState;

        ParticleSystem targetParticleSystem = item.targetComponent as ParticleSystem;

        if (targetParticleSystem != null)
        {
            Debug.Log($"Starting processing on Particle System for parameter: {item.parameterName}");

            var mainModule = targetParticleSystem.main;
            var emissionModule = targetParticleSystem.emission;

            while (elapsedTime < processingState.duration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsedTime / processingState.duration);
                float curveValue = processingState.transitionCurve.Evaluate(normalizedTime);

                // Handle Float Parameters
                if (item.parameterType == ParameterType.Float)
                {
                    //Debug.Log(item.parameterName);
                    if (item.parameterName == "Start Lifetime")
                    {
                        float fromValue = transition.fromState.particleSystemState.FloatSets.Find(p => p.para_name == item.parameterName).float_para;
                        float toValue = transition.toState.particleSystemState.FloatSets.Find(p => p.para_name == item.parameterName).float_para;
                        float newValue = Mathf.Lerp(fromValue, toValue, curveValue);
                        //Debug.Log($"Setting startLifetime from {fromValue} to {newValue}");
                        mainModule.startLifetime = newValue;
                    }
                    else if (item.parameterName == "Start Speed")
                    {
                        float fromValue = transition.fromState.particleSystemState.FloatSets.Find(p => p.para_name == item.parameterName).float_para;
                        float toValue = transition.toState.particleSystemState.FloatSets.Find(p => p.para_name == item.parameterName).float_para;
                        float newValue = Mathf.Lerp(fromValue, toValue, curveValue);
                        //Debug.Log($"Setting startSpeed from {fromValue} to {newValue}");
                        mainModule.startSpeed = newValue;
                    }
                    else if (item.parameterName == "Start Size")
                    {
                        float fromValue = transition.fromState.particleSystemState.FloatSets.Find(p => p.para_name == item.parameterName).float_para;
                        float toValue = transition.toState.particleSystemState.FloatSets.Find(p => p.para_name == item.parameterName).float_para;
                        float newValue = Mathf.Lerp(fromValue, toValue, curveValue);
                        //Debug.Log($"Setting startSize from {fromValue} to {newValue}");
                        mainModule.startSize = newValue;
                    }
                    else if (item.parameterName == "Gravity Modifier")
                    {
                        float fromValue = transition.fromState.particleSystemState.FloatSets.Find(p => p.para_name == item.parameterName).float_para;
                        float toValue = transition.toState.particleSystemState.FloatSets.Find(p => p.para_name == item.parameterName).float_para;
                        float newValue = Mathf.Lerp(fromValue, toValue, curveValue);
                        //Debug.Log($"Setting gravityModifier from {fromValue} to {newValue}");
                        mainModule.gravityModifier = newValue;
                    }
                    else if (item.parameterName == "Emission RateOverTime")
                    {
                        float fromValue = transition.fromState.particleSystemState.FloatSets.Find(p => p.para_name == item.parameterName).float_para;
                        float toValue = transition.toState.particleSystemState.FloatSets.Find(p => p.para_name == item.parameterName).float_para;
                        float newValue = Mathf.Lerp(fromValue, toValue, curveValue);
                        //Debug.Log($"Setting emission rate over time from {fromValue} to {newValue}");
                        emissionModule.rateOverTime = newValue;
                    }
                }
                // Handle Color Parameters
                else if (item.parameterType == ParameterType.Color)
                {      
                    //Debug.Log(item.parameterName);
                    if (item.parameterName == "Start Color")
                    {   
                         
                        Color fromValue = transition.fromState.particleSystemState.ColorSets.Find(p => p.para_name == item.parameterName).color_para;
                        Color toValue = transition.toState.particleSystemState.ColorSets.Find(p => p.para_name == item.parameterName).color_para;
                        Color newValue = Color.Lerp(fromValue, toValue, curveValue);
                        //Debug.Log($"Setting startColor from {fromValue} to {newValue}");
                        mainModule.startColor = newValue;
                    }
                }
                // Handle Vector3 Parameters
                else if (item.parameterType == ParameterType.Vector3)
                {   
                    Debug.Log(item.parameterName);
                    if (item.parameterName == "Start Size3D")
                    {   
                         
                        Vector3 fromValue = transition.fromState.particleSystemState.Vector3Sets.Find(p => p.para_name == item.parameterName).vector3_para;
                        Vector3 toValue = transition.toState.particleSystemState.Vector3Sets.Find(p => p.para_name == item.parameterName).vector3_para;
                        Vector3 newValue = Vector3.Lerp(fromValue, toValue, curveValue);
                        //Debug.Log($"Setting startSize3D from {fromValue} to {newValue}");
                        mainModule.startSizeXMultiplier = newValue.x;
                        mainModule.startSizeYMultiplier = newValue.y;
                        mainModule.startSizeZMultiplier = newValue.z;
                    }
                    else if (item.parameterName == "Start Rotation3D")
                    {   
                         Debug.Log(item.parameterName);
                        Vector3 fromValue = transition.fromState.particleSystemState.Vector3Sets.Find(p => p.para_name == item.parameterName).vector3_para;
                        Vector3 toValue = transition.toState.particleSystemState.Vector3Sets.Find(p => p.para_name == item.parameterName).vector3_para;
                        Vector3 newValue = Vector3.Lerp(fromValue, toValue, curveValue);
                        //Debug.Log($"Setting startRotation3D from {fromValue} to {newValue}");
                        mainModule.startRotationXMultiplier = newValue.x;
                        mainModule.startRotationYMultiplier = newValue.y;
                        mainModule.startRotationZMultiplier = newValue.z;
                    }
                }
                // Handle Bool Parameters
                else if (item.parameterType == ParameterType.Bool)
                {
                     //Debug.Log(item.parameterName);
                    if (item.parameterName == "Emission Enabled")
                    {   
                        
                        bool fromValue = transition.fromState.particleSystemState.BoolSets.Find(p => p.para_name == item.parameterName).bool_para;
                        bool toValue = transition.toState.particleSystemState.BoolSets.Find(p => p.para_name == item.parameterName).bool_para;
                        bool newValue = curveValue > 0.5f ? toValue : fromValue;
                        //Debug.Log($"Setting emission module enabled to {newValue}");
                        emissionModule.enabled = newValue;
                    }
                }

                yield return null; // Continue on the next frame
            }

            Debug.Log($"Finished processing on Particle System for parameter: {item.parameterName}");
        }
    }




    // Method to process each item in the queue
    
}



public enum ParameterType
{
    Float,
    Color,
    Vector3,
    Int,
    Bool
}

public class ProcessingItem
{
    public Component targetComponent; // The component to modify (e.g., Renderer, Light, etc.)
    public Material targetMaterial; // The material to modify (if applicable)
    public string parameterName; // The name of the parameter (e.g., "_Color", "Intensity", etc.)
    public ParameterType parameterType; // The type of the parameter (float, color, or bool)

    // Constructor for both Component and Material being optional
    public ProcessingItem( string parameterName = null, ParameterType parameterType = ParameterType.Float,Component targetComponent = null, Material targetMaterial = null)
    {
        this.targetComponent = targetComponent;
        this.targetMaterial = targetMaterial;
        this.parameterName = parameterName;
        this.parameterType = parameterType;
    }
}
