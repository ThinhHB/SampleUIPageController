# What's this?
This's UIPageController framework, use for Unity3D. It helps us to fast prototype, arrange UI windows/pages in the game, already take care of common tasks such as Animation transition between pages, Block raycast ..etc. Click the image below to watch the sample video on Youtu:

[![Youtu](https://user-images.githubusercontent.com/9117538/62831342-ba8ccb80-bc47-11e9-8d02-47f4e65a2d76.PNG)](https://www.youtube.com/watch?v=RAg1V3krJDc)

# Prequitesites
- Unity 2019.2
- Add "USE_LOG" to the "Scripting define symbols". Cause I'm using my custom Log.cs (Assets/Standard Assets), and in Editor, we need this USE_LOG flag to let all log messages visible in the Console window

![USE_LOG](https://user-images.githubusercontent.com/9117538/62831372-4868b680-bc48-11e9-9912-ce78fd4081b5.png)

I made the simple MenuUI scene without any line of code, just plug custom components, configs the animation, the element order .. sort of simple Editor steps.

# Some definition

## Page

![Page](https://user-images.githubusercontent.com/9117538/62831500-a26a7b80-bc4a-11e9-8514-a500637db03f.png)

A container, hold all sub-elements such as Header, button, background images ..etc. When a page shows up, it may be a simple popup or a more complicated page which a lot of buttons, images, each of them have a transition (fade, moving). We can define those transition in a page like this

![PageOrder](https://user-images.githubusercontent.com/9117538/62831517-f7a68d00-bc4a-11e9-9f70-5f74b98e4015.png)

## Element

Each of them has a transition (fade, moving, rotate ...), and we want those configurations are easy to tweak huh?

![Element](https://user-images.githubusercontent.com/9117538/62831568-d09c8b00-bc4b-11e9-89e6-cf77384bc77a.png)

## Transition between pages

There're some requirements that we always have to solve when working with UI in the game:

- When a popup, a page shows up, it often has some animations such as moving or fade, not just simple placeholder. The transition must follow easing functions https://easings.net/

- User's input only allows on the last showup popup/window, we have to disable raycast on other windows/popups

- Etc..

The UIPageController already take cares most of these common problems.

# Workflow

## Create UIPageController object

I often create one "parent" canvas for each scene, and each page is an sub-canvas. Attach the UIPageController component to the root canvas object, or an object at the same child-level to other canvas. The Controller component will cache reference to all UIPage and do some init tasks.

![PageController](https://user-images.githubusercontent.com/9117538/62836108-1a0bcb00-bc8a-11e9-88fa-9b1fa2d2099b.png)

## Create Page and Element

It's time to layout-ing your UI objects (panel, buttons, background ..). Each page should have an Canvas component.

![Pages](https://user-images.githubusercontent.com/9117538/62836154-8ab2e780-bc8a-11e9-93c7-6d5a72d5df6f.png)

Add UIElement to each element in a page, config there animation. There're 4 types of animation: Move (in X/Y coordination), Fade (alpha changing). You can tweak the animation type, moving distance, duration. The Show animation is used when the page pop out, and the Hide animation ... you can guess what it is right?

![Element](https://user-images.githubusercontent.com/9117538/62836186-cea5ec80-bc8a-11e9-9d5b-321180e8bee2.png)

Back to the Page component, you can config the order for Element

![PageOrder](https://user-images.githubusercontent.com/9117538/62836229-4aa03480-bc8b-11e9-9268-5575822296b6.png)

## Control transition between pages

Press the Play button at MainMenu page, the mainMenu page goes out and the the LevelSelect page come in. How? Browse to the PlayButton object and read it's OnClick function. It's simple call to the function "UI_CloseSelfAndOpenPage" of the MainMenu's UIpage component then pass the reference to the LevelSelect page

![ButtonToShowPage](https://user-images.githubusercontent.com/9117538/62836326-fea1bf80-bc8b-11e9-8fcd-4380b1ba965e.png)

Currently, there're 5 options to control the transition between pages

![Option](https://user-images.githubusercontent.com/9117538/62836370-43c5f180-bc8c-11e9-9d31-9da47f8020da.png)

They're easy to understand huh?.
Okay, that's almost everything you need to know, play around with the Sample scene in "_Sample/_Scene" folder to know more about this framework.

# And ..
I'm not sure this framework can fit or solve all UI issues in your projects, It just works fine for me and solves almost of my UI problems, hope it helps you too.
