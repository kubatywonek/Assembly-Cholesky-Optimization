.code 

Transpose PROC
    TEST    rdx, rdx
    JLE     @TRANSPOSE_DONE
    TEST    r8, r8
    JLE     @TRANSPOSE_DONE
                                                ; src ptr -> rcx
                                                ; n_rows -> rdx
                                                ; n_cols -> r8
                                                ; dst ptr -> r9
    PUSH    rbp
    MOV     rbp, rsp
    PUSH    rbx
    PUSH    r12
    SUB     rsp, 32                             ; alignment

    XOR     rax, rax                            ; rax = i = 0
        
@TRANSPOSE_i_LOOP:                                        ; i loop for i=0..rows-1
    MOV     rbx, rax                            ; rbx = i
    IMUL    rbx, r8                             ; rbx = i*n_cols
    SHL     rbx, 3                              ; rbx = offset = i*n_cols*8

    LEA     r11, [rcx + rbx]                    ; r11 = src[i,0] adress (LEA)

    XOR     r10, r10                            ; r10 = j = 0       
    @TRANSPOSE_j_LOOP:                                    ; j loop for j=0..cols-1
        VMOVSD   xmm0, qword ptr [r11 + r10*8]      ; xmm0 = src[i*n_cols + j]

        MOV     r12, r10                            ; r12 = j
        IMUL    r12, rdx                            ; r12 = j*n_rows
        ADD     r12, rax                            ; r12 = j*n_rows + i
        SHL     r12, 3                              ; r12 = offset = (j*n_rows + i)*8
        VMOVSD  qword ptr [r9 + r12], xmm0          ; dst[j,i] = xmm0

        INC     r10
        CMP     r10, r8                             ; if(j < n_cols) jump
        JL      @TRANSPOSE_j_LOOP

    INC     rax
    CMP     rax, rdx                            ; if(i < n_rows) jump
    JL      @TRANSPOSE_i_LOOP

    ADD     rsp, 32
    POP     r12
    POP     rbx
    POP     rbp

@TRANSPOSE_DONE:
    RET

Transpose ENDP



Multiply_SSE2 PROC
                                                ; STACK:
    PUSH    rbp                                 ; arg 6
    MOV     rbp, rsp                            ; arg 5
    PUSH    rbx                                 ; /\
    PUSH    rsi                                 ; || shadow space
    PUSH    rdi                                 ; \/
    PUSH    r12                                 ; return address
    PUSH    r13                                 ; RBP <-- rbp
    PUSH    r14                                 ; RBX
    SUB     rsp, 32                             ; ...
                                                ; R14 - 32 <-- rsp

    MOV     rsi, rcx                                    ; A base -> rsi
    MOV     rax, rdx                                    ; A Rows -> rax
                                                        ; A Cols -> r8
    MOV     r12, qword ptr [rbp + 48]                   ; B cols -> r12

    MOV     r13, r9                                     ; B base -> r13
    MOV     rdi, qword ptr [rbp + 56]                   ; C base -> rdi

    TEST    rax, rax
    JLE     @SSE_DONE
    TEST    r8, r8
    JLE     @SSE_DONE
    TEST    r12, r12
    JLE     @SSE_DONE

                              ; Main loops
    XOR     rcx, rcx                                    ; i = 0
@SSE_i_LOOP:
    MOV     rdx, rcx                                    ; rdx = i
    IMUL    rdx, r8                                     ; rdx = i * A cols
    SHL     rdx, 3                                      ; *3
    LEA     rbx, [rsi + rdx]                            ; rbx = A[i,0] adress (LEA)

    MOV     rdx, rcx                                    ; rdx = i
    IMUL    rdx, r12                                    ; rdx = i * B cols
    SHL     rdx, 3                                      ; *3
    LEA     r9, [rdi + rdx]                             ; r9 = C[i,0] adress (LEA)

    XOR     r10, r10                                    ; k = 0
    @SSE_k_LOOP:
        CMP     r10, r8                             ; if(k >= A cols) i++
        JGE     @NEXT_i
                                                
        MOVSD   xmm0, qword ptr [rbx + r10*8]       ; load A[i,k] and duplicate to xmm0
        MOVDDUP xmm0, xmm0                          ; xmm0 = [A[i,k], A[i,k]]

        MOV     r14, r10                            ; r14 = k
        IMUL    r14, r12                            ; r14 = k * B cols
        SHL     r14, 3                              ; *3
        LEA     r11, [r13 + r14]                    ; r11 = B[k,0] adress (LEA)

        XOR     rdx, rdx                            ; j = 0
        @SSE_j_LOOP:
            CMP     rdx, r12                        ; if(j >= B cols) k++
            JGE     @NEXT_k

            MOVUPD  xmm1, xmmword ptr [r11 + rdx*8]     ; load [ B[k,j], B[k,j+1] ]
            MOVUPD  xmm2, xmmword ptr [r9  + rdx*8]     ; load [ C[k,j], C[k,j+1] ]

            MULPD   xmm1, xmm0                          ; xmm1 = [ A[i,k]*B[k,j], A[i,k]*B[k,j+1] ]
            ADDPD   xmm2, xmm1                          ; add to [ C[k,j], C[k,j+1] ]

            MOVUPD  xmmword ptr [r9 + rdx*8], xmm2      ; store at [ C[k,j], C[k,j+1] ]

            ADD     rdx, 2                              ; j += 2 (jump over pair)
            JMP     @SSE_j_LOOP

    @NEXT_k:
        INC     r10
        JMP     @SSE_k_LOOP                        ; repeat k loop

@NEXT_i:
    INC     rcx
    CMP     rcx, rax                                    ; if(i < A rows) repeat
    JL      @SSE_i_LOOP

@SSE_DONE:
    ADD     rsp, 32
    POP     r14
    POP     r13
    POP     r12
    POP     rdi
    POP     rsi
    POP     rbx
    POP     rbp
    RET
Multiply_SSE2 ENDP







Multiply_AVX PROC
                                                ; STACK:
    PUSH    rbp                                 ; arg 6
    MOV     rbp, rsp                            ; arg 5
    PUSH    rbx                                 ; /\
    PUSH    rsi                                 ; || shadow space
    PUSH    rdi                                 ; \/
    PUSH    r12                                 ; return address
    PUSH    r13                                 ; RBP <-- rbp
    PUSH    r14                                 ; RBX
    SUB     rsp, 32                             ; ...
                                                ; R14 - 32 <-- rsp

    MOV     rsi, rcx                                    ; A base -> rsi
    MOV     rax, rdx                                    ; A Rows -> rax
                                                        ; A Cols -> r8
    MOV     r12, qword ptr [rbp + 48]                   ; B cols -> r12

    MOV     r13, r9                                     ; B base -> r13
    MOV     rdi, qword ptr [rbp + 56]                   ; C base -> rdi

    TEST    rax, rax
    JLE     @AVX_DONE
    TEST    r8, r8
    JLE     @AVX_DONE
    TEST    r12, r12
    JLE     @AVX_DONE

                              ; Main loops
    XOR     rcx, rcx                                    ; i = 0
@AVX_i_LOOP:
    MOV     rdx, rcx                                    ; rdx = i
    IMUL    rdx, r8                                     ; rdx = i * A cols
    SHL     rdx, 3                                      ; *3
    LEA     rbx, [rsi + rdx]                            ; rbx = A[i,0] adress (LEA)

    MOV     rdx, rcx                                    ; rdx = i
    IMUL    rdx, r12                                    ; rdx = i * B cols
    SHL     rdx, 3                                      ; *3
    LEA     r9, [rdi + rdx]                             ; r9 = C[i,0] adress (LEA)

    XOR     r10, r10                                    ; k = 0
    @AVX_k_LOOP:
        CMP     r10, r8                             ; if(k >= A cols) i++
        JGE     @NEXT_i
                                                
        MOVSD   xmm0, qword ptr [rbx + r10*8]       ; load A[i,k] to xmm0
        VBROADCASTSD ymm0, xmm0                     ; ymm0 = [A[i,k], A[i,k], A[i,k], A[i,k]]

        MOV     r14, r10                            ; r14 = k
        IMUL    r14, r12                            ; r14 = k * B cols
        SHL     r14, 3                              ; *3
        LEA     r11, [r13 + r14]                    ; r11 = B[k,0] adress (LEA)

        XOR     rdx, rdx                            ; j = 0
        @AVX_j_LOOP:
            CMP     rdx, r12                        ; if(j >= B cols) k++
            JGE     @NEXT_k

            VMOVUPD  ymm1, ymmword ptr [r11 + rdx*8]     ; load [ B[k,j], B[k,j+1], B[k,j+2], B[k,j+3] ]
            VMOVUPD  ymm2, ymmword ptr [r9  + rdx*8]     ; load [ C[k,j], C[k,j+1], C[k,j+2], C[k,j+3] ]

            VFMADD231PD ymm2, ymm1, ymm0                 ; Fused multiply-add (ymm1*ymm0) + ymm2 -> ymm2

            VMOVUPD  ymmword ptr [r9 + rdx*8], ymm2      ; store at [ C[k,j], C[k,j+1] ]

            ADD     rdx, 4                              ; j += 2 (jump over pair)
            JMP     @AVX_j_LOOP

    @NEXT_k:
        INC     r10
        JMP     @AVX_k_LOOP                        ; repeat k loop

@NEXT_i:
    INC     rcx
    CMP     rcx, rax                                    ; if(i < A rows) repeat
    JL      @AVX_i_LOOP

@AVX_DONE:
    ADD     rsp, 32
    POP     r14
    POP     r13
    POP     r12
    POP     rdi
    POP     rsi
    POP     rbx
    POP     rbp
    RET
Multiply_AVX ENDP

end