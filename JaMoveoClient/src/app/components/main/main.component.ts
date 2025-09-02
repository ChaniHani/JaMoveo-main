import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { SignalRService } from '../../services/signalr.service';
import { SongService } from '../../services/song.service';
import { RehearsalSession, SongSearchResult } from '../../models/song.model';

@Component({
  selector: 'app-main',
  imports: [FormsModule],
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent implements OnInit, OnDestroy {
  private readonly songService = inject(SongService);
  private readonly signalRService = inject(SignalRService);
  private readonly router = inject(Router);
  private readonly destroy$ = new Subject<void>();

  readonly authService = inject(AuthService);
  isAdmin = false;
  searchQuery = '';
  searchResults: SongSearchResult[] = [];
  searching = false;
  waitingMessage = 'Waiting for next song...';
  currentSession: RehearsalSession | null = null;
  showCreateSessionButton = false;
  connectedUsers: string[] = [];


  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    this.initializeSignalR();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private async initializeSignalR(): Promise<void> {
    try {
      await this.signalRService.startConnection();
      this.setupSignalRListeners();
      await this.signalRService.joinRehearsal();
    } catch (error) {
      console.error('Failed to initialize SignalR:', error);
      this.waitingMessage = 'שגיאה בחיבור לשרת';
    }
  }

  private setupSignalRListeners(): void {
    const takeUntilDestroy = takeUntil(this.destroy$);

    this.signalRService.sessionCreated
      .pipe(takeUntilDestroy)
      .subscribe((session: any) => this.handleSessionCreated(session));

    this.signalRService.joinedSession
      .pipe(takeUntilDestroy)
      .subscribe((session: any) => this.handleJoinedSession(session));

    this.signalRService.noActiveSession
      .pipe(takeUntilDestroy)
      .subscribe((message: any) => this.handleNoActiveSession(message));

    this.signalRService.newSessionAvailable
      .pipe(takeUntilDestroy)
      .subscribe(() => this.signalRService.joinRehearsal());

    this.signalRService.songSelected
      .pipe(takeUntilDestroy)
      .subscribe(song => this.handleSongSelected(song));

    this.signalRService.sessionEnded
      .pipe(takeUntilDestroy)
      .subscribe(() => this.handleSessionEnded());

    this.signalRService.userJoined
      .pipe(takeUntilDestroy)
      .subscribe((username: any) => this.handleUserJoined(username));

    this.signalRService.userLeft
      .pipe(takeUntilDestroy)
      .subscribe((username: any) => this.handleUserLeft(username));

    this.signalRService.error
      .pipe(takeUntilDestroy)
      .subscribe((error: any) => this.handleError(error));
  }

  private handleSessionCreated(session: RehearsalSession): void {
    this.setSessionState(session, 'חדר חזרות פעיל - חפש שירים');
  }

  private handleJoinedSession(session: RehearsalSession): void {
    const message = this.isAdmin
      ? 'חדר חזרות פעיל - חפש שירים'
      : 'מחובר לחדר - ממתין לבחירת שיר';
    this.setSessionState(session, message);
  }

  private setSessionState(session: RehearsalSession, message: string): void {
    this.currentSession = session;
    this.waitingMessage = message;
    this.showCreateSessionButton = false;
  }

  private handleNoActiveSession(message: string): void {
    this.waitingMessage = message;
    this.showCreateSessionButton = this.isAdmin;
  }

  private handleSongSelected(song: any): void {
    this.router.navigate(['/live']);
  }

  private handleSessionEnded(): void {
    this.currentSession = null;
    this.waitingMessage = 'החזרה הסתיימה';
    this.showCreateSessionButton = this.isAdmin;
  }

  private handleUserJoined(username: string): void {
    if (!this.connectedUsers.includes(username)) {
      this.connectedUsers.push(username);
    }
  }

  private handleUserLeft(username: string): void {
    this.connectedUsers = this.connectedUsers.filter(user => user !== username);
  }

  private handleError(error: string): void {
    console.error('SignalR Error occurred');
    this.waitingMessage = 'שגיאה בתקשורת עם השרת';
  }

  async createSession(): Promise<void> {
    if (this.isAdmin) {
      await this.signalRService.createNewSession();
    }
  }

  searchSongs(): void {
    if (!this.searchQuery.trim()) return;

    this.searching = true;
    this.songService.searchSongs(this.searchQuery)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: results => {
          this.searchResults = results.songs;
          this.router.navigate(['/results'], {
            state: { results: this.searchResults, query: this.searchQuery }
          });
        },
        error: (error) => {
          console.error('Search operation failed:', error);
          this.waitingMessage = 'שגיאה בחיפוש שירים';
        },
        complete: () => this.searching = false
      });
  }

  async logout(): Promise<void> {
    try {
      await this.signalRService.stopConnection();
    } catch (error) {
      console.error('Error disconnecting SignalR:', error);
    }
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
