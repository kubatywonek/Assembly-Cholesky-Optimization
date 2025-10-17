
.code 



Tescik proc
MOV EAX, 1
RET 
Tescik endp 




Transpose PROC
                                                ; n_rows -> rdx
                                                ; n_cols -> r8
    TEST    rdx, rdx
    JLE     @DONE
    TEST    r8, r8
    JLE     @DONE

    MOV     rsi, rcx                            ; src ptr -> rsi
    MOV     rdi, r9                             ; dst ptr -> rdi

    XOR     rcx, rcx                            ; ecx = i = 0
        
@i_LOOP:                                        ; i loop for i=0..rows-1
    MOV     rbx, rcx                            ; rbx = i
    IMUL    rbx, r8                             ; rbx = i*n_cols
    SHL     rbx, 3                              ; rbx = offset = i*n_cols*8

    LEA     r11, [rsi + rbx]                    ; r11 = src[i,0] adress (LEA)
    XOR     r12, r12                            ; r12 = j = 0
          
@j_LOOP:                                        ; j loop for j=0..cols-1
    VMOVSD   xmm0, qword ptr [r11 + r12*8]      ; xmm0 = src[i*n_cols + j]

    MOV     r13, r12                            ; r13 = j
    IMUL    r13, rdx                            ; r13 = j*n_rows
    ADD     r13, rcx                            ; r13 = j*n_rows + i
    SHL     r13, 3                              ; r13 = offset = (j*n_rows + i)*8
    VMOVSD  qword ptr [rdi + r13], xmm0         ; dst[j,i] = xmm0

    INC     r12
    CMP     r12, r8                             ; if j < n_cols then jump
    JL      @j_LOOP

    INC     rcx
    CMP     rcx, rdx                            ; if i < n_rows then jump
    JL      @i_LOOP

@DONE:
    RET

Transpose ENDP




end