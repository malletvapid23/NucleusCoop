#pragma once

extern CRITICAL_SECTION mcs;
extern int fake_x; //Delta X
extern int fake_y;

extern int absolute_x;
extern int absolute_y;

extern int use_absolute_cursor_pos_counter; // 0/1/2/3/... : FALSE, requiredAbsCount : TRUE
extern const int required_abs_count;
extern int origin_x, origin_y;
extern int lastX, lastY;

extern HANDLE allowed_mouse_handle; //We will allow raw input from this mouse handle.
extern HANDLE allowed_keyboard_handle;

extern bool use_absolute_cursor_pos;


void update_absolute_cursor_check();