import CoreGraphics
import Foundation

// Post a genuine mouse click at a screen point. AppleScript's "click at" goes
// through the accessibility layer and resolves to the nearest UI element, which
// for a Unity Game view means the window rather than the point, so nothing in
// the game ever sees it. CGEvent posts at the HID level, which is what an actual
// mouse does.
let args = CommandLine.arguments
guard args.count >= 3, let x = Double(args[1]), let y = Double(args[2]) else {
    print("usage: click <x> <y>"); exit(1)
}
let pt = CGPoint(x: x, y: y)

// Move first. Unity tracks pointer position for hover state, and a click posted
// without a preceding move can be attributed to wherever the cursor last was.
CGEvent(mouseEventSource: nil, mouseType: .mouseMoved, mouseCursorPosition: pt, mouseButton: .left)?.post(tap: .cghidEventTap)
usleep(120_000)
CGEvent(mouseEventSource: nil, mouseType: .leftMouseDown, mouseCursorPosition: pt, mouseButton: .left)?.post(tap: .cghidEventTap)
usleep(60_000)
CGEvent(mouseEventSource: nil, mouseType: .leftMouseUp, mouseCursorPosition: pt, mouseButton: .left)?.post(tap: .cghidEventTap)
print("clicked \(Int(x)),\(Int(y))")

// Build:  swiftc -O Tests~/click.swift -o /tmp/click
// Use:    /tmp/click <screenX> <screenY>
//
// Screen coordinates, origin top-left. To map game coordinates onto the screen,
// read the Game view's origin and scale off a `screencapture -x` of the desktop:
// with the view pinned to 1280x720 at scale s and its top-left at (ox, oy),
// screen = (ox + gameX * s, oy + gameY * s).
//
// Terminal needs Accessibility permission for the events to be delivered.
