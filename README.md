# WhatHappened

**WhatHappened** is a Unity editor plugin that visualizes dependencies between C# types and localizes changes that are detected through a project's local Git repository.

![](https://github.com/michaeljcamara/WhatHappened/blob/master/Images/window1.PNG)
|:---:|
| Default **WhatHappened** window with C# type "*Bomb*" selected |

**WhatHappened** is intended to be used as a debugging tool for Unity developers.  If a new bug is noted in a particular type, then recent changes to that type, or any of its dependencies, are likely responsible.  Trying to find these dependencies and checking for changes (made by yourself or a colleague) can be time consuming, tedious, and error prone.  You might check through your Git log and look through dozens of changed files that may have no relation to the type you're interested in.  You might also use your IDE to manually trace through all the dependencies in a C# type, trying to understand whether a changed file could have impacted it.

**WhatHappened** solves this problem by providing a window that can be accessed directly through the Unity editor ("*Window >> WhatHappened*").  You can select from any of the C# types in your project, which produces a tree showing all of its branching dependencies.  You can further select any commit from your Git repository, which highlights nodes based on the number of changes and "impact strength" of each node.  By left-clicking on a node, you can further see where the changes happened (whether inside a particular method, or outside of a method), and you can click on the buttons in the "details panel" to jump directly to the type or method you're interested in.  There is also a toggle to only show types that have changed since the selected commit, which may dramatically lower the search space for finding changes that could have impacted the selected type.

![selectType](https://github.com/michaeljcamara/WhatHappened/blob/master/Images/selectType.gif)
|:---:|
| User can select any C# type in their project as the root node |

![](https://github.com/michaeljcamara/WhatHappened/blob/master/Images/selectCommit.gif)
|:---:|
| User can select a previous commit to see which types have changed, and where those changes happened |

![](https://github.com/michaeljcamara/WhatHappened/blob/master/Images/hideUnchanged.gif)
|:---:|
| User can hide unchanged types to quickly narrow their search |

## Installation
Copy the **WhatHappened** folder into the */Assets/* directory of your Unity project.  After compilation, you can access the **WhatHappened** window by clicking the "Windows" button in the top toolbar and selecting "WhatHappened."

## Notice
**WhatHappened** is currently in active development, and may not be compatible with all projects (yet!).  It requires the Unity project to use the .NET 4.6 scripting runtime version (to modify this setting, navigate from the Unity editor: "*Edit >> Project Settings >> Player >> Configuration*").  It uses the ![LibGit2Sharp](https://github.com/libgit2/libgit2sharp) library to access Git data, which currently depends on this runtime version.  This project was also tested on Windows devices, but additional support for Linux and Mac may be added in the future.  There are many improvements and fixes planned for this project, so stay tuned!

## Author
 ![Michael Camara](https://github.com/michaeljcamara/)

<!---
![hiding to preserve space ](https://github.com/michaeljcamara/WhatHappened/blob/master/Images/window2.PNG)
|:---:|
| Window showing changes between working directory and selected commit |
![]()
-->
