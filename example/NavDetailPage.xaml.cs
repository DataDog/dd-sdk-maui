/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2016-Present Datadog, Inc.
 */

using DatadogSdk.Maui;

namespace example;

public partial class NavDetailPage : ContentPage
{
    public NavDetailPage()
    {
        InitializeComponent();

        DdRum.AddViewAttribute("navigation_type", "navigation_page");
    }

    private async void OnGoBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync(true);
    }
}
