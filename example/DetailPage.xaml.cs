/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2026-Present Datadog, Inc.
 */

using DatadogSdk.Maui;

namespace example;

public partial class DetailPage : ContentPage
{
    public DetailPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        DdRum.AddViewAttribute("navigation_type", "shell");
    }

    private async void OnGoBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }
}
