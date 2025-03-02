using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionSwitch : MonoBehaviour
{
    private Animator animator;
    private int currentInput = 0;
    private int previousInput = 0; // 保存上一次的输入值

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on this GameObject!");
            enabled = false;
        }
    }

    void Update()
    {
        // 获取用户输入 (1-6)
        int input = 0;
        if (Input.GetKeyDown(KeyCode.Alpha1)) input = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) input = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) input = 3;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) input = 4;
        else if (Input.GetKeyDown(KeyCode.Alpha5)) input = 5;
        else if (Input.GetKeyDown(KeyCode.Alpha6)) input = 6;

        // 检查输入是否发生变化
        if (input != previousInput)
        {
            // 设置动画器参数
            animator.SetInteger("Input", input);
            previousInput = input; // 更新上一次的输入值
        }
    }
}
