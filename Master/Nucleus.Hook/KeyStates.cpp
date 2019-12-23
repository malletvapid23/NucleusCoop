#include "pch.h"
#include "Logging.h"

//(unused from USS)
//UINT16 vkey_state; //Stores the mouse keys (5 of them) and the WASD keys. (1=on, 0=off)

const int vkeys_state_size = 256 / 8;//256 vkeys, 8 bits per byte
BYTE* vkeys_state = new BYTE[vkeys_state_size]();

void setVkeyState(int vkey, bool down)
{
	if (vkey >= 0xFF) return;

	auto x = (vkey / 8);
	BYTE shift = (1 << (vkey % 8));
	if (down)
		vkeys_state[x] |= shift;
	else
		vkeys_state[x] &= (~shift);

	if (vkey == VK_LSHIFT || vkey == VK_RSHIFT) setVkeyState(VK_SHIFT, down);
	else if (vkey == VK_LMENU || vkey == VK_RMENU) setVkeyState(VK_MENU, down);
	else if (vkey == VK_LCONTROL || vkey == VK_RCONTROL) setVkeyState(VK_CONTROL, down);
}

bool is_vkey_down(int vkey)
{
	if (vkey >= 0xFF) return false;
	
	BYTE p = vkeys_state[vkey / 8];
	bool ret = (p & (1 << (vkey % 8))) != 0;

	if (!ret)
	{
		if (vkey == VK_LSHIFT || vkey == VK_RSHIFT) return is_vkey_down(VK_SHIFT);
		if (vkey == VK_LMENU || vkey == VK_RMENU) return is_vkey_down(VK_MENU); //alt
		if (vkey == VK_LCONTROL || vkey == VK_RCONTROL) return is_vkey_down(VK_CONTROL);
	}

	return ret;
}