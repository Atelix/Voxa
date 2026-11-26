/* eslint-disable */
/* tslint:disable */
// @ts-nocheck
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

/** Where the voting period is for a meeting. */
export enum VotingPhase {
  NotYetOpen = "NotYetOpen",
  Advance = "Advance",
  Meeting = "Meeting",
  Ended = "Ended",
}

/** What an import did with the voter list. */
export enum VoterImportMode {
  Replaced = "Replaced",
  Added = "Added",
}

/** How an item ended. Elections and proposals use different values. */
export enum ResultOutcome {
  NoVotes = "NoVotes",
  Adopted = "Adopted",
  Rejected = "Rejected",
  Elected = "Elected",
  NoMajority = "NoMajority",
  Tie = "Tie",
}

export enum OptionRole {
  For = "For",
  Against = "Against",
  Abstain = "Abstain",
  Candidate = "Candidate",
  Blank = "Blank",
}

export enum MinorVoting {
  Guardian = "Guardian",
  Self = "Self",
  None = "None",
}

export enum MeetingStatus {
  Draft = "Draft",
  Ready = "Ready",
  Archived = "Archived",
}

export enum MeetingKind {
  Annual = "Annual",
  Extra = "Extra",
}

export enum MajorityRule {
  Absolute = "Absolute",
  TwoThirds = "TwoThirds",
}

/** An item from the voter's point of view. */
export enum BallotItemStatus {
  Pending = "Pending",
  Open = "Open",
  Voted = "Voted",
  Closed = "Closed",
}

export enum AgendaItemState {
  Pending = "Pending",
  Open = "Open",
  Closed = "Closed",
}

export enum AgendaItemKind {
  Proposal = "Proposal",
  Election = "Election",
}

export interface AdminSessionModel {
  signedIn: boolean;
  /** Mail goes to the in-memory test mailbox instead of Mailgun; demo data may be loaded. */
  trialMode: boolean;
}

export interface AgendaItemModel {
  /** @format int32 */
  id: number;
  /** @format int32 */
  position: number;
  title: string;
  description: string | null;
  kind: AgendaItemKind;
  majority: MajorityRule;
  /** @format int32 */
  seats: number;
  state: AgendaItemState;
  advanceVoting: boolean;
  options: OptionModel[];
  /**
   * Ballots cast so far, over every channel. Only the count; never what was voted.
   * @format int32
   */
  ballots: number;
  /** The item can no longer be edited: it has votes or has been opened. */
  locked: boolean;
}

export interface AgendaItemRequest {
  /**
   * @minLength 0
   * @maxLength 300
   */
  title: string;
  /**
   * @minLength 0
   * @maxLength 4000
   */
  description?: string | null;
  kind?: AgendaItemKind;
  majority?: MajorityRule;
  /**
   * Election only: the number of seats.
   * @format int32
   * @min 0
   * @max 50
   */
  seats?: number;
  advanceVoting?: boolean;
  /** Election only: the candidates, in ballot order. */
  candidates?: CandidateRequest[];
}

export interface BallotCodeRequest {
  /**
   * @minLength 0
   * @maxLength 32
   */
  code: string;
}

export interface BallotItemModel {
  /** @format int32 */
  id: number;
  /** @format int32 */
  position: number;
  title: string;
  description: string | null;
  kind: AgendaItemKind;
  /** @format int32 */
  seats: number;
  /** An item from the voter's point of view. */
  status: BallotItemStatus;
  options: OptionModel[];
}

export interface BallotSessionModel {
  meetingName: string;
  /** @format date-time */
  startsAt: string;
  location: string | null;
  /** @format date-time */
  advanceOpensAt: string | null;
  /** Where the voting period is for a meeting. */
  phase: VotingPhase;
  items: BallotItemModel[];
}

export interface CandidateRequest {
  /**
   * @minLength 0
   * @maxLength 200
   */
  label: string;
  /**
   * @minLength 0
   * @maxLength 300
   */
  note?: string | null;
}

export interface ChannelCountModel {
  /** @format int32 */
  advance: number;
  /** @format int32 */
  hall: number;
  /** @format int32 */
  paper: number;
  /** @format int32 */
  total: number;
}

/** The club's name, brand and contact address: public, the app themes itself with it. */
export interface ClubBrandModel {
  clubName: string;
  contactEmail: string | null;
  /** The club's language (Voxa.Abstractions.Languages.Supported), for mails and for devices whose language Voxa lacks. */
  language: string;
  primaryColor: string;
  secondaryColor: string;
  headingFont: string;
  bodyFont: string;
  logoDataUrl: string | null;
}

/** Every club setting, for the board's settings page and to copy a club's setup to a new installation. */
export interface ClubSettingsModel {
  clubName: string;
  contactEmail: string | null;
  /** The club's language (Voxa.Abstractions.Languages.Supported), for mails and for devices whose language Voxa lacks. */
  language: string;
  primaryColor: string;
  secondaryColor: string;
  headingFont: string;
  bodyFont: string;
  logoDataUrl: string | null;
  mailFromName: string;
  mailFromAddress: string | null;
  mailReplyTo: string | null;
  /** @format int32 */
  advanceVotingMaxDays: number;
  advanceVotingArticle: string | null;
  minorVoting: MinorVoting;
  /** @format int32 */
  adultAge: number;
  /**
   * Whether the server has a key for the member import API (`Import__ApiKey`), so a member administration can
   * send the member list. The key itself is never sent.
   */
  importApiConfigured: boolean;
  /** The fonts the app bundles; the font settings must be one of these. */
  availableFonts: string[];
}

/**
 * The envelope for successful responses that carry a message next to the data, such as "Item saved".
 * Failures never use it: they are problem details written by Voxa.WebApi.DomainExceptionHandler.
 */
export interface ClubSettingsModelApiResponse {
  /** Every club setting, for the board's settings page and to copy a club's setup to a new installation. */
  data: ClubSettingsModel;
  message: string | null;
}

export interface ClubSettingsRequest {
  /**
   * @minLength 0
   * @maxLength 200
   */
  clubName: string;
  /**
   * @format email
   * @minLength 0
   * @maxLength 320
   */
  contactEmail?: string | null;
  /**
   * One of Voxa.Abstractions.Languages.Supported; empty keeps the default language.
   * @minLength 0
   * @maxLength 8
   */
  language?: string | null;
  /**
   * @minLength 1
   * @pattern ^#[0-9A-Fa-f]{6}$
   */
  primaryColor: string;
  /**
   * @minLength 1
   * @pattern ^#[0-9A-Fa-f]{6}$
   */
  secondaryColor: string;
  /**
   * @minLength 0
   * @maxLength 60
   */
  headingFont: string;
  /**
   * @minLength 0
   * @maxLength 60
   */
  bodyFont: string;
  /**
   * A data URL of at most about 750 kB.
   * @minLength 0
   * @maxLength 1000000
   */
  logoDataUrl?: string | null;
  /**
   * @minLength 0
   * @maxLength 200
   */
  mailFromName: string;
  /**
   * @format email
   * @minLength 0
   * @maxLength 320
   */
  mailFromAddress?: string | null;
  /**
   * @format email
   * @minLength 0
   * @maxLength 320
   */
  mailReplyTo?: string | null;
  /**
   * @format int32
   * @min 0
   * @max 365
   */
  advanceVotingMaxDays?: number;
  /**
   * @minLength 0
   * @maxLength 100
   */
  advanceVotingArticle?: string | null;
  minorVoting?: MinorVoting;
  /**
   * @format int32
   * @min 12
   * @max 21
   */
  adultAge?: number;
}

/** Everything worth keeping after the season: per meeting, the totals of every item. */
export interface ExportModel {
  /** @format date-time */
  exportedAt: string;
  meetings: MeetingExportModel[];
}

export interface ItemOrderRequest {
  itemIds: number[];
}

export interface LiveItemModel {
  /** @format int32 */
  id: number;
  /** @format int32 */
  position: number;
  title: string;
  kind: AgendaItemKind;
  state: AgendaItemState;
  advanceVoting: boolean;
  ballots: ChannelCountModel;
}

/** The live meeting as the board sees it: turnout only, results only for closed items. */
export interface LiveModel {
  /** A meeting in the board's list. */
  meeting: MeetingSummaryModel;
  advanceOpenNow: boolean;
  /** @format int32 */
  screenItemId: number | null;
  items: LiveItemModel[];
}

export interface MeetingExportModel {
  /** A meeting in the board's list. */
  meeting: MeetingSummaryModel;
  results: ResultModel[];
}

/** A meeting with its agenda, for the board. */
export interface MeetingModel {
  /** @format int32 */
  id: number;
  name: string;
  kind: MeetingKind;
  /** @format date-time */
  startsAt: string;
  location: string | null;
  /** The language of the meeting's mails; empty means the club's language. */
  language: string | null;
  status: MeetingStatus;
  advanceVoting: boolean;
  /** @format date-time */
  advanceOpensAt: string | null;
  /** @format int32 */
  itemCount: number;
  /** @format int32 */
  voterCount: number;
  /** @format int32 */
  codesIssued: number;
  /**
   * Codes that voted at least once; known after archiving, or live while the codes exist.
   * @format int32
   */
  codesUsed: number;
  /** @format date-time */
  archivedAt: string | null;
  /**
   * The earliest moment advance voting may open: 30 days before the meeting (statutes art. 20.8).
   * @format date-time
   */
  earliestAdvanceOpensAt: string;
  /** @format int32 */
  screenItemId: number | null;
  items: AgendaItemModel[];
}

export interface MeetingRequest {
  /**
   * @minLength 0
   * @maxLength 200
   */
  name: string;
  kind?: MeetingKind;
  /** @format date-time */
  startsAt: string;
  /**
   * @minLength 0
   * @maxLength 300
   */
  location?: string | null;
  /**
   * One of Voxa.Abstractions.Languages.Supported for the meeting's mails, or empty for the club's language.
   * @minLength 0
   * @maxLength 8
   */
  language?: string | null;
  advanceVoting?: boolean;
  /**
   * When advance voting opens; empty means the earliest moment the statutes allow.
   * @format date-time
   */
  advanceOpensAt?: string | null;
}

/** A meeting in the board's list. */
export interface MeetingSummaryModel {
  /** @format int32 */
  id: number;
  name: string;
  kind: MeetingKind;
  /** @format date-time */
  startsAt: string;
  location: string | null;
  /** The language of the meeting's mails; empty means the club's language. */
  language: string | null;
  status: MeetingStatus;
  advanceVoting: boolean;
  /** @format date-time */
  advanceOpensAt: string | null;
  /** @format int32 */
  itemCount: number;
  /** @format int32 */
  voterCount: number;
  /** @format int32 */
  codesIssued: number;
  /**
   * Codes that voted at least once; known after archiving, or live while the codes exist.
   * @format int32
   */
  codesUsed: number;
  /** @format date-time */
  archivedAt: string | null;
}

export interface OptionModel {
  /** @format int32 */
  id: number;
  label: string;
  note: string | null;
  role: OptionRole;
}

export interface OptionResultModel {
  /** @format int32 */
  optionId: number;
  label: string;
  role: OptionRole;
  votes: ChannelCountModel;
  elected: boolean;
}

export interface OutboxMailModel {
  to: string;
  subject: string;
  text: string;
  /** @format date-time */
  sentAt: string;
}

/** Fresh codes for paper slips. They are shown once and stored only as hashes. */
export interface PaperCodesModel {
  codes: string[];
}

export interface PaperCodesRequest {
  /**
   * @format int32
   * @min 1
   * @max 500
   */
  count?: number;
}

export interface PaperOptionCount {
  /** @format int32 */
  optionId?: number;
  /**
   * @format int32
   * @min 0
   * @max 100000
   */
  count?: number;
}

export interface PaperVotesRequest {
  /**
   * The number of paper ballots for this item.
   * @format int32
   * @min 0
   * @max 100000
   */
  ballots?: number;
  options: PaperOptionCount[];
}

/** The totals of a closed item. */
export interface ResultModel {
  /** @format int32 */
  itemId: number;
  /** @format int32 */
  meetingId: number;
  /** @format int32 */
  position: number;
  title: string;
  kind: AgendaItemKind;
  majority: MajorityRule;
  /** @format int32 */
  seats: number;
  ballots: ChannelCountModel;
  options: OptionResultModel[];
  /**
   * The votes that count: for plus against, or the ballots that are not blank.
   * @format int32
   */
  validVotes: number;
  /**
   * The number of votes needed: more than half, or two thirds, of Voxa.Abstractions.Models.ResultModel.ValidVotes.
   * @format int32
   */
  votesNeeded: number;
  /** How an item ended. Elections and proposals use different values. */
  outcome: ResultOutcome;
}

/** What the hall screen shows: the open item and its count, or a result the board put on screen. */
export interface ScreenModel {
  meetingName: string;
  openItem: LiveItemModel;
  /** The totals of a closed item. */
  result: ResultModel;
}

export interface ScreenRequest {
  /**
   * A closed item to show on the hall screen, or empty to show the open item again.
   * @format int32
   */
  itemId?: number | null;
}

export interface SendCodesResultModel {
  /** @format int32 */
  sent: number;
  /** @format int32 */
  failed: number;
  /**
   * Voters without an e-mail address; they need a paper code.
   * @format int32
   */
  withoutEmail: number;
}

export interface SignInRequest {
  /**
   * @minLength 0
   * @maxLength 200
   */
  password: string;
}

export interface TestMailRequest {
  /**
   * @format email
   * @minLength 0
   * @maxLength 320
   */
  email: string;
}

export interface VoteRequest {
  /**
   * @minLength 0
   * @maxLength 32
   */
  code: string;
  /** @format int32 */
  itemId?: number;
  /** The chosen option, or for an election the chosen candidates or only the blank option. */
  optionIds: number[];
}

export interface VoterImportRequest {
  /**
   * The member list as CSV text (comma or semicolon), with a header row. See docs/voxa.md.
   * @minLength 0
   * @maxLength 2000000
   */
  csv: string;
}

export interface VoterImportResultModel {
  /** What an import did with the voter list. */
  mode: VoterImportMode;
  /**
   * Voters put on the list: the whole list when replaced, the new voters when added.
   * @format int32
   */
  imported: number;
  /**
   * When adding: members who were already on the list (by name) and were left as they were.
   * @format int32
   */
  alreadyListed: number;
  /** @format int32 */
  viaGuardian: number;
  /** @format int32 */
  withoutEmail: number;
  /** Rows that were left out, such as suspended members, with the reason. */
  skipped: string[];
}

export interface VoterListModel {
  /** @format int32 */
  total: number;
  /** @format int32 */
  viaGuardian: number;
  /** @format int32 */
  withoutEmail: number;
  /** @format int32 */
  codesSent: number;
  voters: VoterModel[];
}

export interface VoterModel {
  /** @format int32 */
  id: number;
  name: string;
  email: string | null;
  guardianName: string | null;
  codeSent: boolean;
}

export interface VoterRequest {
  /**
   * @minLength 0
   * @maxLength 200
   */
  name: string;
  /**
   * @format email
   * @minLength 0
   * @maxLength 320
   */
  email?: string | null;
  /**
   * @minLength 0
   * @maxLength 200
   */
  guardianName?: string | null;
}

export interface WipeRequest {
  /**
   * Must be the confirm word (WISSEN in Dutch, WIPE in English), so nobody wipes the data by accident.
   * @minLength 1
   */
  confirm: string;
}

export type QueryParamsType = Record<string | number, any>;
export type ResponseFormat = keyof Omit<Body, "body" | "bodyUsed">;

export interface FullRequestParams extends Omit<RequestInit, "body"> {
  /** set parameter to `true` for call `securityWorker` for this request */
  secure?: boolean;
  /** request path */
  path: string;
  /** content type of request body */
  type?: ContentType;
  /** query params */
  query?: QueryParamsType;
  /** format of response (i.e. response.json() -> format: "json") */
  format?: ResponseFormat;
  /** request body */
  body?: unknown;
  /** base url */
  baseUrl?: string;
  /** request cancellation token */
  cancelToken?: CancelToken;
}

export type RequestParams = Omit<
  FullRequestParams,
  "body" | "method" | "query" | "path"
>;

export interface ApiConfig<SecurityDataType = unknown> {
  baseUrl?: string;
  baseApiParams?: Omit<RequestParams, "baseUrl" | "cancelToken" | "signal">;
  securityWorker?: (
    securityData: SecurityDataType | null,
  ) => Promise<RequestParams | void> | RequestParams | void;
  customFetch?: typeof fetch;
}

export interface HttpResponse<D extends unknown, E extends unknown = unknown>
  extends Response {
  data: D;
  error: E;
}

type CancelToken = Symbol | string | number;

export enum ContentType {
  Json = "application/json",
  JsonApi = "application/vnd.api+json",
  FormData = "multipart/form-data",
  UrlEncoded = "application/x-www-form-urlencoded",
  Text = "text/plain",
}

export class HttpClient<SecurityDataType = unknown> {
  public baseUrl: string = "";
  private securityData: SecurityDataType | null = null;
  private securityWorker?: ApiConfig<SecurityDataType>["securityWorker"];
  private abortControllers = new Map<CancelToken, AbortController>();
  private customFetch = (...fetchParams: Parameters<typeof fetch>) =>
    fetch(...fetchParams);

  private baseApiParams: RequestParams = {
    credentials: "same-origin",
    headers: {},
    redirect: "follow",
    referrerPolicy: "no-referrer",
  };

  constructor(apiConfig: ApiConfig<SecurityDataType> = {}) {
    Object.assign(this, apiConfig);
  }

  public setSecurityData = (data: SecurityDataType | null) => {
    this.securityData = data;
  };

  protected encodeQueryParam(key: string, value: any) {
    const encodedKey = encodeURIComponent(key);
    return `${encodedKey}=${encodeURIComponent(typeof value === "number" ? value : `${value}`)}`;
  }

  protected addQueryParam(query: QueryParamsType, key: string) {
    return this.encodeQueryParam(key, query[key]);
  }

  protected addArrayQueryParam(query: QueryParamsType, key: string) {
    const value = query[key];
    return value.map((v: any) => this.encodeQueryParam(key, v)).join("&");
  }

  protected toQueryString(rawQuery?: QueryParamsType): string {
    const query = rawQuery || {};
    const keys = Object.keys(query).filter(
      (key) => "undefined" !== typeof query[key],
    );
    return keys
      .map((key) =>
        Array.isArray(query[key])
          ? this.addArrayQueryParam(query, key)
          : this.addQueryParam(query, key),
      )
      .join("&");
  }

  protected addQueryParams(rawQuery?: QueryParamsType): string {
    const queryString = this.toQueryString(rawQuery);
    return queryString ? `?${queryString}` : "";
  }

  private contentFormatters: Record<ContentType, (input: any) => any> = {
    [ContentType.Json]: (input: any) =>
      input !== null && (typeof input === "object" || typeof input === "string")
        ? JSON.stringify(input)
        : input,
    [ContentType.JsonApi]: (input: any) =>
      input !== null && (typeof input === "object" || typeof input === "string")
        ? JSON.stringify(input)
        : input,
    [ContentType.Text]: (input: any) =>
      input !== null && typeof input !== "string"
        ? JSON.stringify(input)
        : input,
    [ContentType.FormData]: (input: any) => {
      if (input instanceof FormData) {
        return input;
      }

      return Object.keys(input || {}).reduce((formData, key) => {
        const property = input[key];
        formData.append(
          key,
          property instanceof Blob
            ? property
            : typeof property === "object" && property !== null
              ? JSON.stringify(property)
              : `${property}`,
        );
        return formData;
      }, new FormData());
    },
    [ContentType.UrlEncoded]: (input: any) => this.toQueryString(input),
  };

  protected mergeRequestParams(
    params1: RequestParams,
    params2?: RequestParams,
  ): RequestParams {
    return {
      ...this.baseApiParams,
      ...params1,
      ...(params2 || {}),
      headers: {
        ...(this.baseApiParams.headers || {}),
        ...(params1.headers || {}),
        ...((params2 && params2.headers) || {}),
      },
    };
  }

  protected createAbortSignal = (
    cancelToken: CancelToken,
  ): AbortSignal | undefined => {
    if (this.abortControllers.has(cancelToken)) {
      const abortController = this.abortControllers.get(cancelToken);
      if (abortController) {
        return abortController.signal;
      }
      return void 0;
    }

    const abortController = new AbortController();
    this.abortControllers.set(cancelToken, abortController);
    return abortController.signal;
  };

  public abortRequest = (cancelToken: CancelToken) => {
    const abortController = this.abortControllers.get(cancelToken);

    if (abortController) {
      abortController.abort();
      this.abortControllers.delete(cancelToken);
    }
  };

  public request = async <T = any, E = any>({
    body,
    secure,
    path,
    type,
    query,
    format,
    baseUrl,
    cancelToken,
    ...params
  }: FullRequestParams): Promise<HttpResponse<T, E>> => {
    const secureParams =
      ((typeof secure === "boolean" ? secure : this.baseApiParams.secure) &&
        this.securityWorker &&
        (await this.securityWorker(this.securityData))) ||
      {};
    const requestParams = this.mergeRequestParams(params, secureParams);
    const queryString = query && this.toQueryString(query);
    const payloadFormatter = this.contentFormatters[type || ContentType.Json];
    const responseFormat = format || requestParams.format;

    return this.customFetch(
      `${baseUrl || this.baseUrl || ""}${path}${queryString ? `?${queryString}` : ""}`,
      {
        ...requestParams,
        headers: {
          ...(requestParams.headers || {}),
          ...(type && type !== ContentType.FormData
            ? { "Content-Type": type }
            : {}),
        },
        signal:
          (cancelToken
            ? this.createAbortSignal(cancelToken)
            : requestParams.signal) || null,
        body:
          typeof body === "undefined" || body === null
            ? null
            : payloadFormatter(body),
      },
    ).then(async (response) => {
      const r = response as HttpResponse<T, E>;
      r.data = null as unknown as T;
      r.error = null as unknown as E;

      const responseToParse = responseFormat ? response.clone() : response;
      const data = !responseFormat
        ? r
        : await responseToParse[responseFormat]()
            .then((data) => {
              if (r.ok) {
                r.data = data;
              } else {
                r.error = data;
              }
              return r;
            })
            .catch((e) => {
              r.error = e;
              return r;
            });

      if (cancelToken) {
        this.abortControllers.delete(cancelToken);
      }

      if (!response.ok) throw data;
      return data;
    });
  };
}

/**
 * @title Voxa
 * @version v1
 */
export class Api<
  SecurityDataType extends unknown,
> extends HttpClient<SecurityDataType> {
  admin = {
    /**
     * No description
     *
     * @tags AdminClub
     * @name AdminClubList
     * @request GET:/api/admin/club
     */
    adminClubList: (params: RequestParams = {}) =>
      this.request<ClubSettingsModel, any>({
        path: `/api/admin/club`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminClub
     * @name AdminClubUpdate
     * @request PUT:/api/admin/club
     */
    adminClubUpdate: (data: ClubSettingsRequest, params: RequestParams = {}) =>
      this.request<ClubSettingsModelApiResponse, any>({
        path: `/api/admin/club`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminData
     * @name AdminDataExportList
     * @summary The totals of every meeting, to keep before the data is wiped.
     * @request GET:/api/admin/data/export
     */
    adminDataExportList: (params: RequestParams = {}) =>
      this.request<ExportModel, any>({
        path: `/api/admin/data/export`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminData
     * @name AdminDataWipeCreate
     * @summary Deletes every meeting, code, voter and total. The club settings stay.
     * @request POST:/api/admin/data/wipe
     */
    adminDataWipeCreate: (data: WipeRequest, params: RequestParams = {}) =>
      this.request<void, any>({
        path: `/api/admin/data/wipe`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminData
     * @name AdminDataDemoCreate
     * @summary Trial setup only: a demo meeting whose codes go to the test mailbox.
     * @request POST:/api/admin/data/demo
     */
    adminDataDemoCreate: (params: RequestParams = {}) =>
      this.request<MeetingModel, any>({
        path: `/api/admin/data/demo`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminData
     * @name AdminDataOutboxList
     * @summary Trial setup only: the mails in the test mailbox, newest first.
     * @request GET:/api/admin/data/outbox
     */
    adminDataOutboxList: (params: RequestParams = {}) =>
      this.request<OutboxMailModel[], any>({
        path: `/api/admin/data/outbox`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminItems
     * @name AdminItemsUpdate
     * @request PUT:/api/admin/items/{id}
     */
    adminItemsUpdate: (
      id: number,
      data: AgendaItemRequest,
      params: RequestParams = {},
    ) =>
      this.request<AgendaItemModel, any>({
        path: `/api/admin/items/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminItems
     * @name AdminItemsDelete
     * @request DELETE:/api/admin/items/{id}
     */
    adminItemsDelete: (id: number, params: RequestParams = {}) =>
      this.request<void, any>({
        path: `/api/admin/items/${id}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminItems
     * @name AdminItemsOpenCreate
     * @request POST:/api/admin/items/{id}/open
     */
    adminItemsOpenCreate: (id: number, params: RequestParams = {}) =>
      this.request<AgendaItemModel, any>({
        path: `/api/admin/items/${id}/open`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminItems
     * @name AdminItemsCloseCreate
     * @request POST:/api/admin/items/{id}/close
     */
    adminItemsCloseCreate: (id: number, params: RequestParams = {}) =>
      this.request<AgendaItemModel, any>({
        path: `/api/admin/items/${id}/close`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminItems
     * @name AdminItemsResultList
     * @summary The result of a closed item. 403 while the item is still open or waiting.
     * @request GET:/api/admin/items/{id}/result
     */
    adminItemsResultList: (id: number, params: RequestParams = {}) =>
      this.request<ResultModel, any>({
        path: `/api/admin/items/${id}/result`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminItems
     * @name AdminItemsPaperUpdate
     * @request PUT:/api/admin/items/{id}/paper
     */
    adminItemsPaperUpdate: (
      id: number,
      data: PaperVotesRequest,
      params: RequestParams = {},
    ) =>
      this.request<ResultModel, any>({
        path: `/api/admin/items/${id}/paper`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminMeetings
     * @name AdminMeetingsList
     * @request GET:/api/admin/meetings
     */
    adminMeetingsList: (params: RequestParams = {}) =>
      this.request<MeetingSummaryModel[], any>({
        path: `/api/admin/meetings`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminMeetings
     * @name AdminMeetingsCreate
     * @request POST:/api/admin/meetings
     */
    adminMeetingsCreate: (data: MeetingRequest, params: RequestParams = {}) =>
      this.request<MeetingModel, any>({
        path: `/api/admin/meetings`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminMeetings
     * @name AdminMeetingsDetail
     * @request GET:/api/admin/meetings/{id}
     */
    adminMeetingsDetail: (id: number, params: RequestParams = {}) =>
      this.request<MeetingModel, any>({
        path: `/api/admin/meetings/${id}`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminMeetings
     * @name AdminMeetingsUpdate
     * @request PUT:/api/admin/meetings/{id}
     */
    adminMeetingsUpdate: (
      id: number,
      data: MeetingRequest,
      params: RequestParams = {},
    ) =>
      this.request<MeetingModel, any>({
        path: `/api/admin/meetings/${id}`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminMeetings
     * @name AdminMeetingsDelete
     * @request DELETE:/api/admin/meetings/{id}
     */
    adminMeetingsDelete: (id: number, params: RequestParams = {}) =>
      this.request<void, any>({
        path: `/api/admin/meetings/${id}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminMeetings
     * @name AdminMeetingsCopyCreate
     * @summary A new draft with the same agenda, a year later.
     * @request POST:/api/admin/meetings/{id}/copy
     */
    adminMeetingsCopyCreate: (id: number, params: RequestParams = {}) =>
      this.request<MeetingModel, any>({
        path: `/api/admin/meetings/${id}/copy`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminMeetings
     * @name AdminMeetingsArchiveCreate
     * @summary Closes every item and deletes the codes and the voter list; only the totals remain.
     * @request POST:/api/admin/meetings/{id}/archive
     */
    adminMeetingsArchiveCreate: (id: number, params: RequestParams = {}) =>
      this.request<MeetingModel, any>({
        path: `/api/admin/meetings/${id}/archive`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminMeetings
     * @name AdminMeetingsItemsCreate
     * @request POST:/api/admin/meetings/{id}/items
     */
    adminMeetingsItemsCreate: (
      id: number,
      data: AgendaItemRequest,
      params: RequestParams = {},
    ) =>
      this.request<AgendaItemModel, any>({
        path: `/api/admin/meetings/${id}/items`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminMeetings
     * @name AdminMeetingsItemsOrderUpdate
     * @request PUT:/api/admin/meetings/{id}/items/order
     */
    adminMeetingsItemsOrderUpdate: (
      id: number,
      data: ItemOrderRequest,
      params: RequestParams = {},
    ) =>
      this.request<AgendaItemModel[], any>({
        path: `/api/admin/meetings/${id}/items/order`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminMeetings
     * @name AdminMeetingsLiveList
     * @summary Turnout per item; results only for closed items.
     * @request GET:/api/admin/meetings/{id}/live
     */
    adminMeetingsLiveList: (id: number, params: RequestParams = {}) =>
      this.request<LiveModel, any>({
        path: `/api/admin/meetings/${id}/live`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminMeetings
     * @name AdminMeetingsScreenUpdate
     * @request PUT:/api/admin/meetings/{id}/screen
     */
    adminMeetingsScreenUpdate: (
      id: number,
      data: ScreenRequest,
      params: RequestParams = {},
    ) =>
      this.request<void, any>({
        path: `/api/admin/meetings/${id}/screen`,
        method: "PUT",
        body: data,
        type: ContentType.Json,
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminSession
     * @name AdminSessionList
     * @request GET:/api/admin/session
     */
    adminSessionList: (params: RequestParams = {}) =>
      this.request<AdminSessionModel, any>({
        path: `/api/admin/session`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminSession
     * @name AdminSessionCreate
     * @request POST:/api/admin/session
     */
    adminSessionCreate: (data: SignInRequest, params: RequestParams = {}) =>
      this.request<AdminSessionModel, any>({
        path: `/api/admin/session`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminSession
     * @name AdminSessionDelete
     * @request DELETE:/api/admin/session
     */
    adminSessionDelete: (params: RequestParams = {}) =>
      this.request<AdminSessionModel, any>({
        path: `/api/admin/session`,
        method: "DELETE",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminVoters
     * @name AdminMeetingsVotersList
     * @request GET:/api/admin/meetings/{meetingId}/voters
     */
    adminMeetingsVotersList: (meetingId: number, params: RequestParams = {}) =>
      this.request<VoterListModel, any>({
        path: `/api/admin/meetings/${meetingId}/voters`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminVoters
     * @name AdminMeetingsVotersCreate
     * @request POST:/api/admin/meetings/{meetingId}/voters
     */
    adminMeetingsVotersCreate: (
      meetingId: number,
      data: VoterRequest,
      params: RequestParams = {},
    ) =>
      this.request<VoterModel, any>({
        path: `/api/admin/meetings/${meetingId}/voters`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminVoters
     * @name AdminMeetingsVotersImportCreate
     * @request POST:/api/admin/meetings/{meetingId}/voters/import
     */
    adminMeetingsVotersImportCreate: (
      meetingId: number,
      data: VoterImportRequest,
      params: RequestParams = {},
    ) =>
      this.request<VoterImportResultModel, any>({
        path: `/api/admin/meetings/${meetingId}/voters/import`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminVoters
     * @name AdminMeetingsVotersDelete
     * @request DELETE:/api/admin/meetings/{meetingId}/voters/{voterId}
     */
    adminMeetingsVotersDelete: (
      voterId: number,
      meetingId: string,
      params: RequestParams = {},
    ) =>
      this.request<void, any>({
        path: `/api/admin/meetings/${meetingId}/voters/${voterId}`,
        method: "DELETE",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminVoters
     * @name AdminMeetingsVotersSendCreate
     * @request POST:/api/admin/meetings/{meetingId}/voters/send
     */
    adminMeetingsVotersSendCreate: (
      meetingId: number,
      params: RequestParams = {},
    ) =>
      this.request<SendCodesResultModel, any>({
        path: `/api/admin/meetings/${meetingId}/voters/send`,
        method: "POST",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminVoters
     * @name AdminMeetingsVotersPaperCreate
     * @summary Codes for paper slips, shown once.
     * @request POST:/api/admin/meetings/{meetingId}/voters/paper
     */
    adminMeetingsVotersPaperCreate: (
      meetingId: number,
      data: PaperCodesRequest,
      params: RequestParams = {},
    ) =>
      this.request<PaperCodesModel, any>({
        path: `/api/admin/meetings/${meetingId}/voters/paper`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags AdminVoters
     * @name AdminMeetingsVotersTestCreate
     * @request POST:/api/admin/meetings/{meetingId}/voters/test
     */
    adminMeetingsVotersTestCreate: (
      meetingId: number,
      data: TestMailRequest,
      params: RequestParams = {},
    ) =>
      this.request<void, any>({
        path: `/api/admin/meetings/${meetingId}/voters/test`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),
  };
  ballot = {
    /**
     * No description
     *
     * @tags Ballot
     * @name BallotSessionCreate
     * @summary The meeting and its items as this code sees them: open, voted, waiting or closed.
     * @request POST:/api/ballot/session
     */
    ballotSessionCreate: (
      data: BallotCodeRequest,
      params: RequestParams = {},
    ) =>
      this.request<BallotSessionModel, any>({
        path: `/api/ballot/session`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags Ballot
     * @name BallotVotesCreate
     * @request POST:/api/ballot/votes
     */
    ballotVotesCreate: (data: VoteRequest, params: RequestParams = {}) =>
      this.request<void, any>({
        path: `/api/ballot/votes`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        ...params,
      }),
  };
  club = {
    /**
     * No description
     *
     * @tags Club
     * @name ClubList
     * @request GET:/api/club
     */
    clubList: (params: RequestParams = {}) =>
      this.request<ClubBrandModel, any>({
        path: `/api/club`,
        method: "GET",
        format: "json",
        ...params,
      }),
  };
  screen = {
    /**
     * No description
     *
     * @tags Screen
     * @name ScreenDetail
     * @request GET:/api/screen/{meetingId}
     */
    screenDetail: (meetingId: number, params: RequestParams = {}) =>
      this.request<ScreenModel, any>({
        path: `/api/screen/${meetingId}`,
        method: "GET",
        format: "json",
        ...params,
      }),
  };
}
