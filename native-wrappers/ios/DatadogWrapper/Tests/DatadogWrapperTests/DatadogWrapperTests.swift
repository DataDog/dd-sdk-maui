import XCTest
@testable import DatadogWrapper

final class DatadogWrapperTests: XCTestCase {

    func testInitialize_withGrantedConsent_returnsTrue() {
        let result = DatadogWrapper.initialize(
            clientToken: "pub-test-token-000000000000000000",
            environment: "test",
            service: "test-service",
            site: "us1",
            verbosity: "error",
            trackingConsent: "granted",
            batchSize: nil,
            uploadFrequency: nil,
            batchProcessingLevel: nil,
            additionalConfiguration: nil
        )
        XCTAssertTrue(result)
    }

    func testInitialize_withNullableParams_returnsTrue() {
        let result = DatadogWrapper.initialize(
            clientToken: "pub-test-token-000000000000000000",
            environment: "test",
            service: nil,
            site: "us1",
            verbosity: "error",
            trackingConsent: "granted",
            batchSize: nil,
            uploadFrequency: nil,
            batchProcessingLevel: nil,
            additionalConfiguration: nil
        )
        XCTAssertTrue(result)
    }

    func testInitialize_withAllSiteValues_returnsTrue() {
        let sites = ["us1", "us3", "us5", "eu1", "ap1", "us1_fed"]
        for site in sites {
            // Note: Datadog SDK may log warnings about re-initialization
            let result = DatadogWrapper.initialize(
                clientToken: "pub-test-token-000000000000000000",
                environment: "test",
                service: nil,
                site: site,
                verbosity: "error",
                trackingConsent: "granted",
                batchSize: nil,
                uploadFrequency: nil,
                batchProcessingLevel: nil,
                additionalConfiguration: nil
            )
            XCTAssertTrue(result, "Failed for site: \(site)")
        }
    }
}
